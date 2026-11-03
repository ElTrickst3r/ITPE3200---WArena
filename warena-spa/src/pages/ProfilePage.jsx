import { useState } from 'react'
import { useAuth } from '../context/AuthContext'

export default function ProfilePage(){
  const {user,updateUsername,changePassword}=useAuth()
  const [username,setUsername]=useState(user?.username||'')
  const [passwords,setPasswords]=useState({currentPassword:'',newPassword:'',confirmPassword:''})
  const [message,setMessage]=useState(''),[error,setError]=useState('')

  async function saveUsername(e){
    e.preventDefault();setError('');setMessage('')
    try{await updateUsername(username.trim());setMessage('Username updated.')}catch(err){setError(err.message)}
  }
  async function savePassword(e){
    e.preventDefault();setError('');setMessage('')
    if(passwords.newPassword!==passwords.confirmPassword){setError('New passwords do not match.');return}
    try{await changePassword(passwords.currentPassword,passwords.newPassword);setPasswords({currentPassword:'',newPassword:'',confirmPassword:''});setMessage('Password changed.')}catch(err){setError(err.message)}
  }

  return <><div className="section-title"><div><h1>My profile</h1><p>Change your username or password.</p></div></div>{message&&<div className="success-box">{message}</div>}{error&&<div className="error-box">{error}</div>}<div className="settings-grid"><section className="form-card"><h2>Username</h2><p>Your username must be unique. Your login email stays the same.</p><form onSubmit={saveUsername}><label>Email<input value={user?.email||''} disabled/></label><label>Username<input required minLength="3" maxLength="30" pattern="[A-Za-z0-9._-]+" value={username} onChange={e=>setUsername(e.target.value)}/></label><button className="primary-button">Save username</button></form></section><section className="form-card"><h2>Password</h2><p>Enter your current password before choosing a new one.</p><form onSubmit={savePassword}><label>Current password<input type="password" required value={passwords.currentPassword} onChange={e=>setPasswords({...passwords,currentPassword:e.target.value})}/></label><label>New password<input type="password" required minLength="8" value={passwords.newPassword} onChange={e=>setPasswords({...passwords,newPassword:e.target.value})}/></label><label>Confirm new password<input type="password" required minLength="8" value={passwords.confirmPassword} onChange={e=>setPasswords({...passwords,confirmPassword:e.target.value})}/></label><button className="primary-button">Change password</button></form></section></div></>
}
