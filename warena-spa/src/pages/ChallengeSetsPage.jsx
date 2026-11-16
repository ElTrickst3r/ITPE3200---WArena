import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { challengeSetService } from '../services/contentService'

// Students only see published sets here; drafts are managed under Manage → Content.
const EMPTY_FILTERS = { search: '', minQuestions: '', isPublished: 'true' }

export default function ChallengeSetsPage() {
  const [sets, setSets] = useState([])
  const [filters, setFilters] = useState(EMPTY_FILTERS)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const update = field => e => setFilters({ ...filters, [field]: e.target.value })

  async function search(activeFilters) {
    setLoading(true)
    try {
      setSets(await challengeSetService.search(activeFilters))
      setError('')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    search(EMPTY_FILTERS)
  }, [])

  const resetFilters = () => {
    setFilters(EMPTY_FILTERS)
    search(EMPTY_FILTERS)
  }

  return (
    <>
      <div className="section-title">
        <div>
          <h1>Challenge sets</h1>
          <p>Choose a named collection such as HTML Basics or CSS Fundamentals.</p>
        </div>
      </div>

      <form className="filter-panel compact" onSubmit={e => { e.preventDefault(); search(filters) }}>
        <label>
          Search
          <input value={filters.search} onChange={update('search')} placeholder="Name or description" />
        </label>
        <label>
          Minimum questions
          <input type="number" min="1" value={filters.minQuestions} onChange={update('minQuestions')} />
        </label>
        <button className="primary-button">Filter</button>
        <button type="button" className="secondary-button" onClick={resetFilters}>Reset</button>
      </form>

      {error && <div className="error-box">{error}</div>}

      {loading ? (
        <p>Loading challenge sets…</p>
      ) : sets.length === 0 ? (
        <p>No challenge sets match your filters.</p>
      ) : (
        <div className="card-grid">
          {sets.map(s => (
            <article className="card" key={s.id}>
              <span className="pill">{s.questionCount} questions</span>
              <h3>{s.name}</h3>
              <p>{s.description}</p>
              <div className="card-meta">
                <span>{s.possibleXp} possible XP</span>
              </div>
              <Link className="primary-button" to={`/sets/${s.id}/play`}>Open set</Link>
            </article>
          ))}
        </div>
      )}
    </>
  )
}