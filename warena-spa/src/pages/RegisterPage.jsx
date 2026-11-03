import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function RegisterPage(){
 const {register}=useAuth();const navigate=useNavigate();const [form,setForm]=useState({username:'',email:'',password:''});const [error,setError]=useState('')
 async function submit(e){e.preventDefault();setError('');if(form.username.length<3){setError('Username must be at least 3 characters.');return}if(form.password.length<8){setError('Password must be at least 8 characters.');return}try{await register(form.username,form.email,form.password);navigate('/')}catch(err){setError(err.message)}}
 return <div className="auth-panel"><span className="eyebrow">STUDENT ACCOUNT</span><h1>Create account</h1><p>Register to save XP, streaks and badges.</p>{error&&<div className="error-box">{error}</div>}<form onSubmit={submit}><label>Username<input required minLength="3" maxLength="30" pattern="[A-Za-z0-9._-]+" value={form.username} onChange={e=>setForm({...form,username:e.target.value})}/></label><label>Email<input type="email" required value={form.email} onChange={e=>setForm({...form,email:e.target.value})}/></label><label>Password<input type="password" required minLength="8" value={form.password} onChange={e=>setForm({...form,password:e.target.value})}/></label><button className="primary-button">Register</button></form><small>Already registered? <Link to="/login">Log in</Link>.</small></div>
}
