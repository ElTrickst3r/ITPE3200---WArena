import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../context/AuthContext'
import { challengeSetService } from '../services/contentService'
import { connectToRoom, liveService } from '../services/liveService'
import JoinRoomCard from '../components/live/JoinRoomCard'
import CreateRoomCard from '../components/live/CreateRoomCard'
import RoomView from '../components/live/RoomView'

export default function LiveArenaPage() {
  const { user, canManage } = useAuth()
  const [sets, setSets] = useState([])
  const [room, setRoom] = useState(null)
  const [automation, setAutomation] = useState({ enabled: false, delaySeconds: 3 })
  const [answer, setAnswer] = useState('')
  const [answerInfo, setAnswerInfo] = useState(null)
  const [remaining, setRemaining] = useState(0)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const questionKey = room?.currentQuestion?.id
  const startedRef = useRef(Date.now())

  // Teachers and admins need the list of published sets to create a room.
  useEffect(() => {
    if (!canManage) return
    challengeSetService.search({ isPublished: 'true' }).then(setSets).catch(err => setError(err.message))
  }, [canManage])

  // Receive live updates from the server while in a room.
  useEffect(() => {
    if (!room?.code) return
    const connection = connectToRoom(room.code, setRoom, setError)
    return () => { connection.stop() }
  }, [room?.code])

  // Reset the answer and restart the countdown whenever a new question starts.
  useEffect(() => {
    if (!room?.currentQuestion) {
      setRemaining(0)
      return
    }
    startedRef.current = Date.now()
    setAnswer('')
    setAnswerInfo(null)
    setRemaining(room.currentQuestion.timeLimitSeconds)
    const timer = setInterval(() => setRemaining(value => Math.max(0, value - 0.1)), 100)
    return () => clearInterval(timer)
  }, [questionKey])

  // Runs a room action, clears old errors first and shows a new one if the action fails.
  // Returns the result, or undefined if the action failed.
  async function run(action) {
    setError('')
    try {
      return await action()
    } catch (err) {
      setError(err.message)
    }
  }

  async function createRoom(form) {
    const state = await run(() => liveService.createRoom({
      challengeSetId: Number(form.challengeSetId),
      theme: form.theme,
      timeLimitSeconds: Number(form.timeLimitSeconds)
    }))
    if (!state) return
    setAutomation({ enabled: form.autoContinue, delaySeconds: Number(form.autoContinueDelaySeconds) })
    setRoom(state)
  }

  async function joinRoom(code) {
    const state = await run(() => liveService.joinRoom(code))
    if (!state) return
    setAutomation({ enabled: false, delaySeconds: 3 })
    setRoom(state)
  }

  async function start() {
    const state = await run(() => liveService.start(room.code))
    if (state) setRoom(state)
  }

  async function next() {
    const state = await run(() => liveService.next(room.code))
    if (state) setRoom(state)
  }

  async function submitAnswer(e) {
    e.preventDefault()
    if (!answer || remaining <= 0) return
    setBusy(true)
    // The server measures response time itself; this value is only sent because the API expects it.
    const elapsedMs = Math.min(60000, Math.max(0, Date.now() - startedRef.current))
    const result = await run(() => liveService.answer(room.code, answer, elapsedMs))
    setBusy(false)
    if (!result) return
    setAnswerInfo({ score: result.score })
    setRoom(result.state)
  }

  function leave() {
    setRoom(null)
    setError('')
  }

  if (room) {
    return (
      <RoomView
        room={room}
        user={user}
        canManage={canManage}
        automation={automation}
        answer={answer}
        setAnswer={setAnswer}
        answerInfo={answerInfo}
        remaining={remaining}
        busy={busy}
        onSubmit={submitAnswer}
        onStart={start}
        onNext={next}
        onLeave={leave}
        error={error}
      />
    )
  }

  return (
    <>
      <div className="section-title">
        <div>
          <h1>Live Arena</h1>
          <p>Join a room and compete in real time. Correctness comes first; faster correct answers earn a bigger time bonus.</p>
        </div>
      </div>
      {error && <div className="error-box">{error}</div>}
      <div className="live-entry-grid">
        <JoinRoomCard onJoin={joinRoom} />
        {canManage && <CreateRoomCard sets={sets} onCreate={createRoom} />}
      </div>
    </>
  )
}