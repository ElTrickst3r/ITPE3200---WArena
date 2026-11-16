import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { PASSWORD_HINT, validatePassword } from '../utils/passwordRules'

// Returns an error message for the first rule that fails, or an empty string if the form is valid.
function validate(form) {
    if (form.username.trim().length < 3) return 'Username must be at least 3 characters.'
    return validatePassword(form.password)
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
                    <small>{PASSWORD_HINT}</small>
                </label>
                <button className="primary-button" disabled={busy}>{busy ? 'Creating account…' : 'Register'}</button>
            </form>
            <small>Already registered? <Link to="/login">Log in</Link>.</small>
        </div>
    )
}