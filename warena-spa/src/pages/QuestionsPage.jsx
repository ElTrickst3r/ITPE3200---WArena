import { useEffect, useState } from 'react'
import { categoryService, questionService } from '../services/contentService'
import PracticeDialog from '../components/PracticeDialog'

const EMPTY_FILTERS = { search: '', categoryId: '', difficulty: '', minPoints: '', maxPoints: '' }

export default function QuestionsPage() {
  const [questions, setQuestions] = useState([])
  const [categories, setCategories] = useState([])
  const [filters, setFilters] = useState(EMPTY_FILTERS)
  const [selected, setSelected] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const update = field => e => setFilters({ ...filters, [field]: e.target.value })

  async function search(activeFilters) {
    const { minPoints, maxPoints } = activeFilters
    if (minPoints !== '' && maxPoints !== '' && Number(minPoints) > Number(maxPoints)) {
      setError('Min XP cannot be higher than max XP.')
      return
    }

    setLoading(true)
    try {
      setQuestions(await questionService.search(activeFilters))
      setError('')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    categoryService.getAll().then(setCategories).catch(err => setError(err.message))
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
          <h1>Individual questions</h1>
          <p>Pick anything from the question bank and practice it on its own.</p>
        </div>
      </div>

      <form className="filter-panel" onSubmit={e => { e.preventDefault(); search(filters) }}>
        <label>
          Search
          <input value={filters.search} onChange={update('search')} placeholder="Title or question" />
        </label>
        <label>
          Category
          <select value={filters.categoryId} onChange={update('categoryId')}>
            <option value="">All</option>
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </label>
        <label>
          Difficulty
          <select value={filters.difficulty} onChange={update('difficulty')}>
            <option value="">All</option>
            <option>Easy</option>
            <option>Medium</option>
            <option>Hard</option>
          </select>
        </label>
        <label>
          Min XP
          <input type="number" min="0" value={filters.minPoints} onChange={update('minPoints')} />
        </label>
        <label>
          Max XP
          <input type="number" min="0" value={filters.maxPoints} onChange={update('maxPoints')} />
        </label>
        <button className="primary-button">Filter</button>
        <button type="button" className="secondary-button" onClick={resetFilters}>Reset</button>
      </form>

      {error && <div className="error-box">{error}</div>}

      {loading ? (
        <p>Loading questions…</p>
      ) : questions.length === 0 ? (
        <p>No questions match your filters.</p>
      ) : (
        <div className="card-grid">
          {questions.map(q => (
            <article className="card" key={q.id}>
              <div className="card-top">
                <span className="pill">{q.category}</span>
                <span className={`difficulty ${q.difficulty.toLowerCase()}`}>{q.difficulty}</span>
              </div>
              <h3>{q.title}</h3>
              <p>{q.prompt}</p>
              <div className="card-meta">
                <span>{q.points} XP</span>
                <span>{q.type === 'MultipleChoice' ? 'Multiple choice' : 'Short answer'}</span>
              </div>
              <button type="button" className="primary-button" onClick={() => setSelected(q)}>Practice</button>
            </article>
          ))}
        </div>
      )}

      {selected && <PracticeDialog key={selected.id} question={selected} onClose={() => setSelected(null)} />}
    </>
  )
}