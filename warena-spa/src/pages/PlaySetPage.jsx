import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { challengeSetService } from '../services/contentService'
import { progressService } from '../services/progressService'

export default function PlaySetPage() {
  const { id } = useParams()
  const [set, setSet] = useState(null)
  const [loadError, setLoadError] = useState('')
  const [index, setIndex] = useState(0)
  const [answer, setAnswer] = useState('')
  const [result, setResult] = useState(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [score, setScore] = useState({ correct: 0, xp: 0 })

  useEffect(() => {
    challengeSetService.get(id).then(setSet).catch(err => setLoadError(err.message))
  }, [id])

  if (loadError) return <div className="error-box">{loadError}</div>
  if (!set) return <p>Loading…</p>
  if (set.questions.length === 0) {
    return (
      <div className="empty">
        <h2>No questions in this set</h2>
        <Link className="secondary-button" to="/sets">Back to challenge sets</Link>
      </div>
    )
  }

  const total = set.questions.length
  if (index >= total) return <FinishCard set={set} score={score} total={total} />

  const question = set.questions[index]
  const isLast = index + 1 === total

  async function submit(e) {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      const response = await progressService.answerQuestion(question.id, answer)
      setResult(response)
      if (response.isCorrect) {
        setScore(s => ({ correct: s.correct + 1, xp: s.xp + response.pointsEarned }))
      }
    } catch (err) {
      // Shown above the answer field so the user can try again without losing their progress in the set.
      setError(err.message)
    } finally {
      setBusy(false)
    }
  }

  function next() {
    setIndex(index + 1)
    setAnswer('')
    setResult(null)
    setError('')
  }

  return (
    <div className="play-layout">
      <div className="play-progress">
        <div>
          <strong>{set.name}</strong>
          <span>Question {index + 1} of {total}</span>
        </div>
        <div className="progress-track">
          <span style={{ width: `${((index + 1) / total) * 100}%` }} />
        </div>
      </div>

      <article className="play-card">
        <div className="card-top">
          <span className="pill">{question.category}</span>
          <span className={`difficulty ${question.difficulty.toLowerCase()}`}>{question.difficulty}</span>
        </div>
        <h1>{question.title}</h1>
        <p className="question-text">{question.prompt}</p>
        {error && <div className="error-box">{error}</div>}

        {result ? (
          <div className={`result-box ${result.isCorrect ? 'success' : 'failure'}`}>
            <strong>{result.isCorrect ? 'Correct!' : 'Not quite'}</strong>
            <span>{result.isCorrect ? `+${result.pointsEarned} XP` : '0 XP'}</span>
            <button type="button" className="primary-button" onClick={next}>
              {isLast ? 'Finish set' : 'Next question'}
            </button>
          </div>
        ) : (
          <form onSubmit={submit}>
            {question.type === 'MultipleChoice' ? (
              <div className="option-list">
                {question.options.map(option => (
                  <label className="option" key={option}>
                    <input required type="radio" name="answer" value={option} checked={answer === option} onChange={e => setAnswer(e.target.value)} />
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
            <div className="submit-row">
              <span>{question.points} XP</span>
              <button className="primary-button" disabled={busy}>{busy ? 'Submitting…' : 'Submit'}</button>
            </div>
          </form>
        )}
      </article>
    </div>
  )
}

function FinishCard({ set, score, total }) {
  return (
    <div className="finish-card">
      <span className="podium-medal">🏁</span>
      <h1>{set.name} complete</h1>
      <p>You answered {score.correct} of {total} questions correctly and earned {score.xp} XP. Your answers are saved in History.</p>
      <div className="actions">
        <Link className="primary-button" to="/sets">More challenge sets</Link>
        <Link className="secondary-button" to="/history">View history</Link>
      </div>
    </div>
  )
}