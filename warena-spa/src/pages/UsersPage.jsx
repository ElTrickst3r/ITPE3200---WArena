import { useEffect, useState } from 'react'
import { useAuth } from '../context/AuthContext'
import { adminService } from '../services/adminService'
import { PASSWORD_HINT, validatePassword } from '../utils/passwordRules'

export default function UsersPage() {
  const { user: currentUser } = useAuth()
  const [users, setUsers] = useState([])
  const [editing, setEditing] = useState(null)
  const [busy, setBusy] = useState(false)
  // Only one message is shown at a time: { type: 'success' | 'error', text }.
  const [status, setStatus] = useState(null)

  async function load() {
    try {
      setUsers(await adminService.getUsers())
    } catch (err) {
      setStatus({ type: 'error', text: err.message })
    }
  }

  useEffect(() => {
    load()
  }, [])

  // Close the edit panel with Escape.
  useEffect(() => {
    if (!editing) return
    const onKeyDown = e => {
      if (e.key === 'Escape') setEditing(null)
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [editing])

  const update = field => e => setEditing({ ...editing, [field]: e.target.value })

  function beginEdit(user) {
    setStatus(null)
    setEditing({ id: user.id, email: user.email, username: user.username, role: user.role, newPassword: '' })
  }

  async function save(e) {
    e.preventDefault()
    if (editing.newPassword) {
      const passwordError = validatePassword(editing.newPassword)
      if (passwordError) {
        setStatus({ type: 'error', text: passwordError })
        return
      }
    }

    setStatus(null)
    setBusy(true)
    try {
      await adminService.updateUser(editing.id, {
        username: editing.username.trim(),
        role: editing.role,
        newPassword: editing.newPassword || null
      })
      setEditing(null)
      setStatus({ type: 'success', text: 'User updated.' })
      await load()
    } catch (err) {
      setStatus({ type: 'error', text: err.message })
    } finally {
      setBusy(false)
    }
  }

  async function remove(user) {
    if (!window.confirm(`Delete ${user.username}? Their progress, badges and related data will also be removed.`)) return
    setStatus(null)
    try {
      await adminService.deleteUser(user.id)
      setStatus({ type: 'success', text: 'User deleted.' })
      await load()
    } catch (err) {
      setStatus({ type: 'error', text: err.message })
    }
  }

  return (
    <>
      <div className="section-title">
        <div>
          <h1>User administration</h1>
          <p>Manage usernames, passwords and roles, or delete accounts.</p>
        </div>
      </div>

      {status && <div className={status.type === 'success' ? 'success-box' : 'error-box'}>{status.text}</div>}

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Username</th>
              <th>XP</th>
              <th>Role</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {users.map(user => (
              <tr key={user.id}>
                <td>
                  <strong>{user.username}</strong>
                  <small className="block">{user.email}</small>
                </td>
                <td>{user.totalXp}</td>
                <td>{user.role}</td>
                <td>
                  {user.id === currentUser?.userId ? (
                    <span className="muted-note">Use Profile for your own account</span>
                  ) : (
                    <div className="row-actions">
                      <button type="button" className="secondary-button" onClick={() => beginEdit(user)}>Edit</button>
                      <button type="button" className="danger-button" onClick={() => remove(user)}>Delete</button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {editing && (
        <div className="modal-panel">
          <form className="form-card" onSubmit={save}>
            <div className="section-title">
              <div>
                <h2>Edit {editing.username}</h2>
                <p>{editing.email}</p>
              </div>
              <button type="button" className="secondary-button" onClick={() => setEditing(null)}>Close</button>
            </div>
            <label>
              Username
              <input required minLength="3" maxLength="30" pattern="[A-Za-z0-9._-]+" title="Letters, digits, dots, underscores and hyphens only"
                value={editing.username} onChange={update('username')} />
            </label>
            <label>
              Role
              <select value={editing.role} onChange={update('role')}>
                <option>Student</option>
                <option>Teacher</option>
                <option>Admin</option>
              </select>
            </label>
            <label>
              New password (optional)
              <input type="password" autoComplete="new-password" value={editing.newPassword} onChange={update('newPassword')}
                placeholder="Leave blank to keep current password" />
              <small>{PASSWORD_HINT}</small>
            </label>
            <button className="primary-button" disabled={busy}>{busy ? 'Saving…' : 'Save changes'}</button>
          </form>
        </div>
      )}
    </>
  )
}