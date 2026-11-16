import { useCallback, useEffect, useState } from 'react'
import { categoryService, challengeSetService, questionService } from '../services/contentService'
import CategoryManager from '../components/manage/CategoryManager'
import QuestionManager from '../components/manage/QuestionManager'
import ChallengeSetManager from '../components/manage/ChallengeSetManager'

const TABS = [
  { key: 'questions', label: 'Questions' },
  { key: 'sets', label: 'Challenge Sets' },
  { key: 'categories', label: 'Categories' }
]

export default function ManagePage() {
  const [tab, setTab] = useState('questions')
  const [categories, setCategories] = useState([])
  const [questions, setQuestions] = useState([])
  const [sets, setSets] = useState([])
  // Only one message is shown at a time: { type: 'success' | 'error', text }.
  const [status, setStatus] = useState(null)

  const load = useCallback(async () => {
    try {
      const [c, q, s] = await Promise.all([
        categoryService.getAll(),
        questionService.getAll(),
        challengeSetService.getAll()
      ])
      setCategories(c)
      setQuestions(q)
      setSets(s)
    } catch (err) {
      setStatus({ type: 'error', text: err.message })
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  // All lists are reloaded after a change, since a change in one tab can affect the others.
  const handlers = {
    onSaved: async text => {
      setStatus({ type: 'success', text })
      await load()
    },
    onError: text => setStatus({ type: 'error', text }),
    clearStatus: () => setStatus(null)
  }

  const switchTab = key => {
    setTab(key)
    setStatus(null)
  }

  return (
    <>
      <div className="section-title">
        <div>
          <h1>Content management</h1>
          <p>Teachers and admins can manage the reusable question bank and challenge sets.</p>
        </div>
      </div>

      <div className="tabs">
        {TABS.map(t => (
          <button key={t.key} type="button" className={tab === t.key ? 'active' : ''} onClick={() => switchTab(t.key)}>
            {t.label}
          </button>
        ))}
      </div>

      {status && <div className={status.type === 'success' ? 'success-box' : 'error-box'}>{status.text}</div>}

      {tab === 'questions' && <QuestionManager questions={questions} categories={categories} {...handlers} />}
      {tab === 'sets' && <ChallengeSetManager sets={sets} questions={questions} {...handlers} />}
      {tab === 'categories' && <CategoryManager categories={categories} {...handlers} />}
    </>
  )
}