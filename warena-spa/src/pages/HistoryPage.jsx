import { useEffect, useState } from 'react'
import { progressService } from '../services/progressService'

export default function HistoryPage() {
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    progressService.getHistory()
      .then(setItems)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  return (
    <>
      <div className="section-title">
        <div>
          <h1>Submission history</h1>
          <p>Review your previous attempts and earned points.</p>
        </div>
      </div>

      {error && <div className="error-box">{error}</div>}

      {loading ? (
        <p>Loading history…</p>
      ) : items.length === 0 ? (
        <div className="empty">
          <h2>No submissions yet</h2>
          <p>Answer a question or play a challenge set to see your history here.</p>
        </div>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Question</th>
                <th>Category</th>
                <th>Answer</th>
                <th>Result</th>
                <th>XP</th>
                <th>Date</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  <td>{item.question}</td>
                  <td>{item.category}</td>
                  <td>{item.answer}</td>
                  <td>
                    <span className={`status ${item.isCorrect ? 'ok' : 'bad'}`}>{item.isCorrect ? 'Correct' : 'Incorrect'}</span>
                  </td>
                  <td>{item.pointsEarned}</td>
                  <td>{new Date(item.submittedAtUtc).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}