import { useEffect, useState } from 'react'
import { progressService } from '../services/progressService'

export default function AchievementsPage() {
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    progressService.getAchievements()
      .then(setItems)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  return (
    <>
      <div className="section-title">
        <div>
          <h1>Achievement badges</h1>
          <p>Badges reward milestones beyond your total XP.</p>
        </div>
      </div>

      {error && <div className="error-box">{error}</div>}

      {loading ? (
        <p>Loading badges…</p>
      ) : items.length === 0 ? (
        <div className="empty">
          <h2>No badges yet</h2>
          <p>Your first correct answer unlocks the first one.</p>
        </div>
      ) : (
        <div className="badge-grid">
          {items.map(badge => (
            <article className="badge-card" key={badge.id}>
              <span>{badge.icon}</span>
              <div>
                <strong>{badge.name}</strong>
                <p>{badge.description}</p>
                <small>{new Date(badge.earnedAtUtc).toLocaleDateString()}</small>
              </div>
            </article>
          ))}
        </div>
      )}
    </>
  )
}