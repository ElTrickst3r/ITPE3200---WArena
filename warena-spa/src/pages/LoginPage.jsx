import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ email: '', password: '' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const update = field => e => setForm({ ...form, [field]: e.target.value })

  async function submit(e) {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      await login(form.email.trim(), form.password)
      navigate('/')
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-panel">
      <span className="eyebrow">WArena SPA</span>
      <h1>Log in</h1>
      <p>Continue your course challenges and progress.</p>
      {error && <div className="error-box">{error}</div>}
      <form onSubmit={submit}>
        <label>
          Email
          <input type="email" required autoComplete="email" value={form.email} onChange={update('email')} />
        </label>
        <label>
          Password
          <input type="password" required autoComplete="current-password" value={form.password} onChange={update('password')} />
        </label>
        <button className="primary-button" disabled={busy}>{busy ? 'Logging in…' : 'Log in'}</button>
      </form>
      <small>New here? <Link to="/register">Create a student account</Link>.</small>
    </div>
  )
}