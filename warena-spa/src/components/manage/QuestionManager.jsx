import { useState } from 'react'
import { questionService } from '../../services/contentService'
import ManageList from './ManageList'

const OPTION_KEYS = ['optionA', 'optionB', 'optionC', 'optionD']
const EMPTY_QUESTION = {
  title: '', prompt: '', correctAnswer: '', type: 'ShortAnswer', difficulty: 'Easy',
  points: 100, categoryId: '', optionA: '', optionB: '', optionC: '', optionD: ''
}

// Mirrors the multiple-choice rules in the API so the user gets feedback before submitting.
function validate(form) {
  if (form.type !== 'MultipleChoice') return ''
  const options = OPTION_KEYS.map(key => form[key].trim())
  if (options.some(option => !option)) return 'Multiple-choice questions need four options.'
  const correct = form.correctAnswer.trim().toLowerCase()
  if (!options.some(option => option.toLowerCase() === correct)) return 'The correct answer must match one of the options.'
  return ''
}

export default function QuestionManager({ questions, categories, onSaved, onError, clearStatus }) {
  const [form, setForm] = useState(EMPTY_QUESTION)
  const [editingId, setEditingId] = useState(null)
  const [busy, setBusy] = useState(false)

  const update = field => e => setForm({ ...form, [field]: e.target.value })
  const reset = () => {
    setForm(EMPTY_QUESTION)
    setEditingId(null)
  }

  async function save(e) {
    e.preventDefault()
    clearStatus()
    const validationError = validate(form)
    if (validationError) {
      onError(validationError)
      return
    }

    const body = { ...form, points: Number(form.points), categoryId: Number(form.categoryId) }
    setBusy(true)
    try {
      if (editingId) await questionService.update(editingId, body)
      else await questionService.create(body)
      reset()
      await onSaved('Question saved.')
    } catch (err) {
      onError(err.message)
    } finally {
      setBusy(false)
    }
  }

  async function edit(id) {
    try {
      const q = await questionService.getForEdit(id)
      setEditingId(id)
      setForm({
        title: q.title,
        prompt: q.prompt,
        correctAnswer: q.correctAnswer,
        type: q.type,
        difficulty: q.difficulty,
        points: q.points,
        categoryId: q.categoryId,
        optionA: q.optionA || '',
        optionB: q.optionB || '',
        optionC: q.optionC || '',
        optionD: q.optionD || ''
      })
    } catch (err) {
      onError(err.message)
    }
  }

  async function remove(id) {
    if (!window.confirm('Delete this question?')) return
    clearStatus()
    try {
      await questionService.remove(id)
      if (editingId === id) reset()
      await onSaved('Question deleted.')
    } catch (err) {
      onError(err.message)
    }
  }

  return (
    <div className="manage-grid">
      <form className="form-card" onSubmit={save}>
        <h2>{editingId ? 'Edit question' : 'New question'}</h2>
        <label>
          Title
          <input required maxLength="120" value={form.title} onChange={update('title')} />
        </label>
        <label>
          Question
          <textarea required maxLength="1200" value={form.prompt} onChange={update('prompt')} />
        </label>
        <div className="two">
          <label>
            Category
            <select required value={form.categoryId} onChange={update('categoryId')}>
              <option value="">Choose</option>
              {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </label>
          <label>
            Difficulty
            <select value={form.difficulty} onChange={update('difficulty')}>
              <option>Easy</option>
              <option>Medium</option>
              <option>Hard</option>
            </select>
          </label>
        </div>
        <div className="two">
          <label>
            Type
            <select value={form.type} onChange={update('type')}>
              <option>ShortAnswer</option>
              <option>MultipleChoice</option>
            </select>
          </label>
          <label>
            XP
            <input required type="number" min="10" max="1000" value={form.points} onChange={update('points')} />
          </label>
        </div>
        <label>
          Correct answer
          <input required maxLength="500" value={form.correctAnswer} onChange={update('correctAnswer')} />
        </label>
        {form.type === 'MultipleChoice' && (
          <div className="two">
            {OPTION_KEYS.map((key, i) => (
              <label key={key}>
                Option {String.fromCharCode(65 + i)}
                <input required maxLength="250" value={form[key]} onChange={update(key)} />
              </label>
            ))}
          </div>
        )}
        <div className="actions">
          <button className="primary-button" disabled={busy}>{busy ? 'Saving…' : 'Save'}</button>
          {editingId && <button type="button" className="secondary-button" onClick={reset}>Cancel</button>}
        </div>
      </form>

      <ManageList
        title="Questions"
        items={questions}
        emptyText="No questions yet."
        renderInfo={q => <><strong>{q.title}</strong><small>{q.category} · {q.difficulty} · {q.points} XP</small></>}
        onEdit={edit}
        onDelete={remove}
      />
    </div>
  )
}