import { useState } from 'react'
import { challengeSetService } from '../../services/contentService'
import ManageList from './ManageList'

const EMPTY_SET = { name: '', description: '', isPublished: true, questionIds: [] }

export default function ChallengeSetManager({ sets, questions, onSaved, onError, clearStatus }) {
  const [form, setForm] = useState(EMPTY_SET)
  const [editingId, setEditingId] = useState(null)
  const [busy, setBusy] = useState(false)

  const update = field => e => setForm({ ...form, [field]: e.target.value })
  const reset = () => {
    setForm(EMPTY_SET)
    setEditingId(null)
  }

  // Adds or removes a question while keeping the order the questions were selected in.
  const toggleQuestion = (id, checked) => setForm({
    ...form,
    questionIds: checked ? [...form.questionIds, id] : form.questionIds.filter(q => q !== id)
  })

  async function save(e) {
    e.preventDefault()
    clearStatus()
    if (form.questionIds.length === 0) {
      onError('Choose at least one question.')
      return
    }

    setBusy(true)
    try {
      if (editingId) await challengeSetService.update(editingId, form)
      else await challengeSetService.create(form)
      reset()
      await onSaved('Challenge set saved.')
    } catch (err) {
      onError(err.message)
    } finally {
      setBusy(false)
    }
  }

  async function edit(id) {
    try {
      const set = await challengeSetService.get(id)
      setEditingId(id)
      setForm({
        name: set.name,
        description: set.description || '',
        isPublished: set.isPublished,
        questionIds: set.questions.map(q => q.id)
      })
    } catch (err) {
      onError(err.message)
    }
  }

  async function remove(id) {
    if (!window.confirm('Delete this challenge set?')) return
    clearStatus()
    try {
      await challengeSetService.remove(id)
      if (editingId === id) reset()
      await onSaved('Challenge set deleted.')
    } catch (err) {
      onError(err.message)
    }
  }

  return (
    <div className="manage-grid">
      <form className="form-card" onSubmit={save}>
        <h2>{editingId ? 'Edit challenge set' : 'New challenge set'}</h2>
        <label>
          Name
          <input required maxLength="100" value={form.name} onChange={update('name')} />
        </label>
        <label>
          Description
          <textarea maxLength="500" value={form.description} onChange={update('description')} />
        </label>
        <label className="check">
          <input type="checkbox" checked={form.isPublished} onChange={e => setForm({ ...form, isPublished: e.target.checked })} />
          {' '}Published
        </label>
        <fieldset>
          <legend>Questions ({form.questionIds.length} selected)</legend>
          <div className="question-checks">
            {questions.map(q => (
              <label className="check-row" key={q.id}>
                <input type="checkbox" checked={form.questionIds.includes(q.id)} onChange={e => toggleQuestion(q.id, e.target.checked)} />
                <span><strong>{q.title}</strong><small>{q.category} · {q.points} XP</small></span>
              </label>
            ))}
          </div>
        </fieldset>
        <div className="actions">
          <button className="primary-button" disabled={busy}>{busy ? 'Saving…' : 'Save'}</button>
          {editingId && <button type="button" className="secondary-button" onClick={reset}>Cancel</button>}
        </div>
      </form>

      <ManageList
        title="Challenge Sets"
        items={sets}
        emptyText="No challenge sets yet."
        renderInfo={s => <><strong>{s.name}</strong><small>{s.questionCount} questions · {s.isPublished ? 'Published' : 'Draft'}</small></>}
        onEdit={edit}
        onDelete={remove}
      />
    </div>
  )
}