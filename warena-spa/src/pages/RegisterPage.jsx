import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

// Mirrors the password rules enforced by ASP.NET Identity in the API.
const PASSWORD_RULES = [
  [p => p.length >= 8, 'at least 8 characters'],
  [p => /[a-z]/.test(p), 'a lowercase letter'],
  [p => /[A-Z]/.test(p), 'an uppercase letter'],
  [p => /\d/.test(p), 'a digit'],
  [p => /[^A-Za-z0-9]/.test(p), 'a special character']
]

// Returns an error message for the first rule that fails, or an empty string if the form is valid.
function validate(form) {
  if (form.username.trim().length < 3) return 'Username must be at least 3 characters.'
  const missing = PASSWORD_RULES.filter(([test]) => !test(form.password)).map(([, text]) => text)
  if (missing.length > 0) return `Password must contain ${missing.join(', ')}.`
  return ''
}

export default function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ username: '', email: '', password: '' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const update = field => e => setForm({ ...form, [field]: e.target.value })

  async function submit(e) {
    e.preventDefault()
    const validationError = validate(form)
    if (validationError) {
      setError(validationError)
      return
    }

    setError('')
    setBusy(true)
    try {
      await register(form.username.trim(), form.email.trim(), form.password)
      navigate('/')
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-panel">
      <span className="eyebrow">STUDENT ACCOUNT</span>
      <h1>Create account</h1>
      <p>Register to save XP, streaks and badges.</p>
      {error && <div className="error-box">{error}</div>}
      <form onSubmit={submit}>
        <label>
          Username
          <input required minLength="3" maxLength="30" pattern="[A-Za-z0-9._-]+" autoComplete="username"
            title="Letters, digits, dots, underscores and hyphens only"
            value={form.username} onChange={update('username')} />
        </label>
        <label>
          Email
          <input type="email" required autoComplete="email" value={form.email} onChange={update('email')} />
        </label>
        <label>
          Password
          <input type="password" required minLength="8" autoComplete="new-password" value={form.password} onChange={update('password')} />
          <small>At least 8 characters, with upper and lower case letters, a digit and a special character.</small>
        </label>
        <button className="primary-button" disabled={busy}>{busy ? 'Creating account…' : 'Register'}</button>
      </form>
      <small>Already registered? <Link to="/login">Log in</Link>.</small>
    </div>
  )
}