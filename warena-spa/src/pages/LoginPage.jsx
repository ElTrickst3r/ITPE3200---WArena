import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function LoginPage(){
  const { login }=useAuth(); const navigate=useNavigate(); const [form,setForm]=useState({email:'student@warena.local',password:'Student123!'}); const [error,setError]=useState(''); const [busy,setBusy]=useState(false)
  async function submit(e){e.preventDefault();setError('');setBusy(true);try{await login(form.email,form.password);navigate('/')}catch(err){setError(err.message)}finally{setBusy(false)}}
  return <div className="auth-panel"><span className="eyebrow">WArena SPA</span><h1>Log in</h1><p>Continue your course challenges and progress.</p>{error&&<div className="error-box">{error}</div>}<form onSubmit={submit}><label>Email<input type="email" required value={form.email} onChange={e=>setForm({...form,email:e.target.value})}/></label><label>Password<input type="password" required value={form.password} onChange={e=>setForm({...form,password:e.target.value})}/></label><button className="primary-button" disabled={busy}>{busy?'Logging in…':'Log in'}</button></form><small>New here? <Link to="/register">Create a student account</Link>.</small></div>
}
