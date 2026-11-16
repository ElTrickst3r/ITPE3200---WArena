import { useState } from 'react'
import { categoryService } from '../../services/contentService'
import ManageList from './ManageList'

const EMPTY_CATEGORY = { name: '', description: '' }

export default function CategoryManager({ categories, onSaved, onError, clearStatus }) {
  const [form, setForm] = useState(EMPTY_CATEGORY)
  const [editingId, setEditingId] = useState(null)
  const [busy, setBusy] = useState(false)

  const update = field => e => setForm({ ...form, [field]: e.target.value })
  const reset = () => {
    setForm(EMPTY_CATEGORY)
    setEditingId(null)
  }

  async function save(e) {
    e.preventDefault()
    clearStatus()
    setBusy(true)
    try {
      if (editingId) await categoryService.update(editingId, form)
      else await categoryService.create(form)
      reset()
      await onSaved('Category saved.')
    } catch (err) {
      onError(err.message)
    } finally {
      setBusy(false)
    }
  }

  async function edit(id) {
    try {
      const category = await categoryService.get(id)
      setEditingId(id)
      setForm({ name: category.name, description: category.description || '' })
    } catch (err) {
      onError(err.message)
    }
  }

  async function remove(id) {
    if (!window.confirm('Delete this category?')) return
    clearStatus()
    try {
      await categoryService.remove(id)
      if (editingId === id) reset()
      await onSaved('Category deleted.')
    } catch (err) {
      onError(err.message)
    }
  }

  return (
    <div className="manage-grid">
      <form className="form-card" onSubmit={save}>
        <h2>{editingId ? 'Edit category' : 'New category'}</h2>
        <label>
          Name
          <input required maxLength="60" value={form.name} onChange={update('name')} />
        </label>
        <label>
          Description
          <textarea maxLength="250" value={form.description} onChange={update('description')} />
        </label>
        <div className="actions">
          <button className="primary-button" disabled={busy}>{busy ? 'Saving…' : 'Save'}</button>
          {editingId && <button type="button" className="secondary-button" onClick={reset}>Cancel</button>}
        </div>
      </form>

      <ManageList
        title="Categories"
        items={categories}
        emptyText="No categories yet."
        renderInfo={c => <><strong>{c.name}</strong><small>{c.description}</small></>}
        onEdit={edit}
        onDelete={remove}
      />
    </div>
  )
}