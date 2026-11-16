import { useState } from 'react'
import { useAuth } from '../context/AuthContext'
import { PASSWORD_HINT, validatePassword } from '../utils/passwordRules'

const EMPTY_PASSWORDS = { currentPassword: '', newPassword: '', confirmPassword: '' }

export default function ProfilePage() {
  const { user, updateUsername, changePassword } = useAuth()
  const [username, setUsername] = useState(user?.username || '')
  const [passwords, setPasswords] = useState(EMPTY_PASSWORDS)
  const [busy, setBusy] = useState(null) // 'username' | 'password' | null
  // Only one message is shown at a time: { type: 'success' | 'error', text }.
  const [status, setStatus] = useState(null)

  const updatePassword = field => e => setPasswords({ ...passwords, [field]: e.target.value })

  async function saveUsername(e) {
    e.preventDefault()
    const trimmed = username.trim()
    if (trimmed === user?.username) {
      setStatus({ type: 'error', text: 'This is already your username.' })
      return
    }

    setStatus(null)
    setBusy('username')
    try {
      await updateUsername(trimmed)
      setStatus({ type: 'success', text: 'Username updated.' })
    } catch (err) {
      setStatus({ type: 'error', text: err.message })
    } finally {
      setBusy(null)
    }
  }

  async function savePassword(e) {
    e.preventDefault()
    if (passwords.newPassword !== passwords.confirmPassword) {
      setStatus({ type: 'error', text: 'New passwords do not match.' })
      return
    }
    const passwordError = validatePassword(passwords.newPassword)
    if (passwordError) {
      setStatus({ type: 'error', text: passwordError })
      return
    }

    setStatus(null)
    setBusy('password')
    try {
      await changePassword(passwords.currentPassword, passwords.newPassword)
      setPasswords(EMPTY_PASSWORDS)
      setStatus({ type: 'success', text: 'Password changed.' })
    } catch (err) {
      setStatus({ type: 'error', text: err.message })
    } finally {
      setBusy(null)
    }
  }

  return (
    <>
      <div className="section-title">
        <div>
          <h1>My profile</h1>
          <p>Change your username or password.</p>
        </div>
      </div>

      {status && <div className={status.type === 'success' ? 'success-box' : 'error-box'}>{status.text}</div>}

      <div className="settings-grid">
        <section className="form-card">
          <h2>Username</h2>
          <p>Your username must be unique. Your login email stays the same.</p>
          <form onSubmit={saveUsername}>
            <label>
              Email
              <input value={user?.email || ''} disabled />
            </label>
            <label>
              Username
              <input required minLength="3" maxLength="30" pattern="[A-Za-z0-9._-]+" autoComplete="username"
                title="Letters, digits, dots, underscores and hyphens only"
                value={username} onChange={e => setUsername(e.target.value)} />
            </label>
            <button className="primary-button" disabled={busy === 'username'}>
              {busy === 'username' ? 'Saving…' : 'Save username'}
            </button>
          </form>
        </section>

        <section className="form-card">
          <h2>Password</h2>
          <p>Enter your current password before choosing a new one.</p>
          <form onSubmit={savePassword}>
            <label>
              Current password
              <input type="password" required autoComplete="current-password" value={passwords.currentPassword} onChange={updatePassword('currentPassword')} />
            </label>
            <label>
              New password
              <input type="password" required minLength="8" autoComplete="new-password" value={passwords.newPassword} onChange={updatePassword('newPassword')} />
              <small>{PASSWORD_HINT}</small>
            </label>
            <label>
              Confirm new password
              <input type="password" required minLength="8" autoComplete="new-password" value={passwords.confirmPassword} onChange={updatePassword('confirmPassword')} />
            </label>
            <button className="primary-button" disabled={busy === 'password'}>
              {busy === 'password' ? 'Saving…' : 'Change password'}
            </button>
          </form>
        </section>
      </div>
    </>
  )
}