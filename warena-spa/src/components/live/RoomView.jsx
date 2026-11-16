import { useEffect, useRef } from 'react'
import RaceBoard from './RaceBoard'
import TopThree from './TopThree'
import FinalPodium from './FinalPodium'

export default function RoomView({ room, user, canManage, automation, answer, setAnswer, answerInfo, remaining, busy, onSubmit, onStart, onNext, onLeave, error }) {
  const joined = room.leaderboard.some(row => row.userId === user?.userId)
  const isHost = room.hostUserId === user?.userId
  const autoTimerRef = useRef(null)
  const scheduledQuestionRef = useRef(null)
  const questionId = room.currentQuestion?.id
  const everyoneAnswered = room.participantCount > 0 && room.currentAnswerCount >= room.participantCount
  const roundReady = remaining <= 0 || everyoneAnswered

  // Cancel a scheduled auto-continue when the question changes.
  useEffect(() => {
    if (scheduledQuestionRef.current !== questionId) {
      if (autoTimerRef.current) clearTimeout(autoTimerRef.current)
      autoTimerRef.current = null
      scheduledQuestionRef.current = null
    }
  }, [questionId])

  // The host's browser moves to the next question once everyone has answered or the time is up.
  // Each question is scheduled at most once, tracked by scheduledQuestionRef.
  useEffect(() => {
    const enabled = isHost && automation.enabled && room.status === 'Running' && Boolean(room.currentQuestion)
    if (!enabled || !roundReady) {
      if (autoTimerRef.current && scheduledQuestionRef.current === questionId) {
        clearTimeout(autoTimerRef.current)
        autoTimerRef.current = null
        scheduledQuestionRef.current = null
      }
      return
    }
    if (scheduledQuestionRef.current === questionId) return

    scheduledQuestionRef.current = questionId
    autoTimerRef.current = setTimeout(() => {
      autoTimerRef.current = null
      onNext()
    }, Math.max(1, automation.delaySeconds) * 1000)
  }, [isHost, automation.enabled, automation.delaySeconds, room.status, questionId, roundReady, onNext, room.currentQuestion])

  // Clear any pending timer when leaving the room.
  useEffect(() => () => {
    if (autoTimerRef.current) clearTimeout(autoTimerRef.current)
  }, [])

  function manualNext() {
    if (autoTimerRef.current) clearTimeout(autoTimerRef.current)
    autoTimerRef.current = null
    scheduledQuestionRef.current = questionId
    onNext()
  }

  if (room.status === 'Completed') return <FinalPodium room={room} onLeave={onLeave} />

  const playerCount = room.leaderboard.length
  const question = room.currentQuestion

  return (
    <>
      <div className="live-header">
        <div>
          <span className="eyebrow">LIVE ARENA · {room.theme.toUpperCase()}</span>
          <h1>{room.challengeSetName}</h1>
          <p>
            Room <strong>{room.code}</strong> · {playerCount} player{playerCount === 1 ? '' : 's'}
            {isHost && automation.enabled ? ` · Auto-continue ${automation.delaySeconds}s` : ''}
          </p>
        </div>
        <div className="live-actions">
          {isHost && room.status === 'Waiting' && (
            <button type="button" className="primary-button" onClick={onStart} disabled={playerCount === 0}>Start game</button>
          )}
          {isHost && room.status === 'Running' && (
            <button type="button" className="primary-button" onClick={manualNext}>Next question</button>
          )}
          <button type="button" className="secondary-button" onClick={onLeave}>Leave</button>
        </div>
      </div>

      {error && <div className="error-box">{error}</div>}

      <RaceBoard room={room} />

      {room.status === 'Waiting' && (
        <div className="waiting-card">
          <span>⏳</span>
          <h2>Waiting for the host</h2>
          {!joined && !canManage
            ? <p>You need to join the room again.</p>
            : <p>The race begins when the teacher starts the game.</p>}
        </div>
      )}

      {room.status === 'Running' && question && (
        <div className="live-play">
          <article className="live-question">
            <div className="question-status">
              <span>Question {question.index + 1} / {question.total}</span>
              <strong className={remaining < 5 ? 'urgent' : ''}>{remaining.toFixed(1)}s</strong>
            </div>
            <h2>{question.title}</h2>
            <p className="question-text">{question.prompt}</p>

            {isHost && !joined ? (
              <div className="host-note">
                Host view: students answer on their own devices.
                {automation.enabled && roundReady ? ` Advancing automatically in ${automation.delaySeconds} seconds.` : ''}
              </div>
            ) : answerInfo ? (
              <div className={`result-box ${answerInfo.score > 0 ? 'success' : 'failure'}`}>
                <strong>Answer locked in</strong>
                <span>+{answerInfo.score} live points</span>
              </div>
            ) : (
              <AnswerForm question={question} answer={answer} setAnswer={setAnswer} remaining={remaining} busy={busy} onSubmit={onSubmit} />
            )}
          </article>
          <TopThree rows={room.leaderboard} />
        </div>
      )}
    </>
  )
}

function AnswerForm({ question, answer, setAnswer, remaining, busy, onSubmit }) {
  const timeUp = remaining <= 0

  return (
    <form onSubmit={onSubmit}>
      {question.type === 'MultipleChoice' ? (
        <div className="option-list">
          {question.options.map(option => (
            <label className="option" key={option}>
              <input required type="radio" name="live-answer" value={option} checked={answer === option} onChange={e => setAnswer(e.target.value)} />
              <span>{option}</span>
            </label>
          ))}
        </div>
      ) : (
        <label>
          Your answer
          <input required maxLength="500" value={answer} onChange={e => setAnswer(e.target.value)} autoFocus />
        </label>
      )}
      <button className="primary-button" disabled={timeUp || busy}>
        {timeUp ? 'Time is up' : busy ? 'Submitting…' : 'Lock in answer'}
      </button>
    </form>
  )
}