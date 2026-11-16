import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { progressService } from '../services/progressService'
import { challengeSetService } from '../services/contentService'

const FEATURED_SET_COUNT = 6

export default function DashboardPage() {
  const { user } = useAuth()
  const [dashboard, setDashboard] = useState(null)
  const [sets, setSets] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([progressService.getDashboard(), challengeSetService.search({ isPublished: 'true' })])
      .then(([stats, publishedSets]) => {
        setDashboard(stats)
        setSets(publishedSets.slice(0, FEATURED_SET_COUNT))
      })
      .catch(err => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  return (
    <>
      {error && <div className="error-box">{error}</div>}

      <section className="hero">
        <span className="eyebrow">WEB APPLICATIONS, GAMIFIED</span>
        <h1>Welcome back, {user?.username}.</h1>
        <p>Practice course material, keep your streak alive, and challenge classmates in the Live Arena.</p>
        <div className="hero-actions">
          <Link className="light-button big" to="/sets">Start a set</Link>
          <Link className="ghost-button big" to="/live">Enter Live Arena</Link>
        </div>
      </section>

      {dashboard && (
        <div className="stats">
          <Stat value={dashboard.totalXp} label="XP earned" />
          <Stat value={dashboard.completedQuestions} label="Questions completed" />
          <Stat value={`${dashboard.accuracy}%`} label="Accuracy" />
          <Stat value={`🔥 ${dashboard.currentStreak}`} label="Day streak" />
        </div>
      )}

      <SectionTitle title="Challenge sets" text="Grouped questions for focused practice" action={<Link to="/sets">Browse all →</Link>} />
      {loading ? (
        <p>Loading…</p>
      ) : sets.length === 0 ? (
        <p>No challenge sets are published yet.</p>
      ) : (
        <div className="card-grid">
          {sets.map(set => (
            <article className="card" key={set.id}>
              <span className="pill">{set.questionCount} questions</span>
              <h3>{set.name}</h3>
              <p>{set.description}</p>
              <div className="card-meta">
                <span>{set.possibleXp} possible XP</span>
              </div>
              <Link className="primary-button" to={`/sets/${set.id}/play`}>Start</Link>
            </article>
          ))}
        </div>
      )}

      {dashboard?.recentAchievements?.length > 0 && (
        <>
          <SectionTitle title="Recent badges" text="Milestones you recently unlocked" />
          <div className="badge-grid">
            {dashboard.recentAchievements.map(badge => (
              <article className="badge-card" key={badge.id}>
                <span>{badge.icon}</span>
                <div>
                  <strong>{badge.name}</strong>
                  <p>{badge.description}</p>
                </div>
              </article>
            ))}
          </div>
        </>
      )}
    </>
  )
}

function Stat({ value, label }) {
  return (
    <div className="stat">
      <strong>{value}</strong>
      <span>{label}</span>
    </div>
  )
}

function SectionTitle({ title, text, action }) {
  return (
    <div className="section-title">
      <div>
        <h2>{title}</h2>
        <p>{text}</p>
      </div>
      {action}
    </div>
  )
}