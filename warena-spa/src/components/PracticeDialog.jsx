import { useEffect, useState } from 'react'
import { progressService } from '../services/progressService'

// Modal for practicing a single question. Closes on Escape or a click outside the dialog.
export default function PracticeDialog({ question, onClose }) {
  const [answer, setAnswer] = useState('')
  const [result, setResult] = useState(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    const onKeyDown = e => {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  async function submit(e) {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      setResult(await progressService.answerQuestion(question.id, answer))
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(false)
    }
  }

  const tryAgain = () => {
    setResult(null)
    setAnswer('')
  }

  return (
    <div className="modal-backdrop" onMouseDown={e => { if (e.target === e.currentTarget) onClose() }}>
      <div className="dialog" role="dialog" aria-modal="true" aria-labelledby="practice-title">
        <button type="button" className="close" aria-label="Close" onClick={onClose}>×</button>
        <span className="pill">{question.category}</span>
        <h2 id="practice-title">{question.title}</h2>
        <p className="question-text">{question.prompt}</p>
        {error && <div className="error-box">{error}</div>}

        {result ? (
          <div className={`result-box ${result.isCorrect ? 'success' : 'failure'}`}>
            <strong>{result.isCorrect ? 'Correct!' : 'Not quite'}</strong>
            <span>{result.isCorrect ? `+${result.pointsEarned} XP` : '0 XP'}</span>
            <button type="button" className="secondary-button" onClick={tryAgain}>Try again</button>
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
            <button className="primary-button" disabled={busy}>{busy ? 'Submitting…' : 'Submit answer'}</button>
          </form>
        )}
      </div>
    </div>
  )
}