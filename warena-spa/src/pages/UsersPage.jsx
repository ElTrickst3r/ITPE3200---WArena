import { useEffect, useState } from 'react'
import { api } from '../services/api'
import { useAuth } from '../context/AuthContext'

export default function UsersPage(){
  const {user:currentUser}=useAuth()
  const [users,setUsers]=useState([]),[editing,setEditing]=useState(null),[error,setError]=useState(''),[message,setMessage]=useState('')
  async function load(){try{setUsers(await api.get('/api/admin/users'))}catch(e){setError(e.message)}}
  useEffect(()=>{load()},[])

  function begin(u){setError('');setMessage('');setEditing({id:u.id,email:u.email,username:u.username,role:u.role,newPassword:''})}
  async function save(e){e.preventDefault();setError('');setMessage('');try{await api.put(`/api/admin/users/${editing.id}`,{username:editing.username,role:editing.role,newPassword:editing.newPassword||null});setEditing(null);setMessage('User updated.');await load()}catch(e){setError(e.message)}}
  async function remove(u){if(!window.confirm(`Delete ${u.username}? Their progress, badges and related data will also be removed.`))return;setError('');setMessage('');try{await api.del(`/api/admin/users/${u.id}`);setMessage('User deleted.');await load()}catch(e){setError(e.message)}}

  return <><div className="section-title"><div><h1>User administration</h1><p>Manage usernames, passwords and roles, or delete accounts.</p></div></div>{message&&<div className="success-box">{message}</div>}{error&&<div className="error-box">{error}</div>}<div className="table-wrap"><table><thead><tr><th>Username</th><th>XP</th><th>Role</th><th>Actions</th></tr></thead><tbody>{users.map(u=><tr key={u.id}><td><strong>{u.username}</strong><small className="block">{u.email}</small></td><td>{u.totalXp}</td><td>{u.role}</td><td>{u.id===currentUser?.userId?<span className="muted-note">Use Profile for your own account</span>:<div className="row-actions"><button className="secondary-button" type="button" onClick={()=>begin(u)}>Edit</button><button className="danger-button" type="button" onClick={()=>remove(u)}>Delete</button></div>}</td></tr>)}</tbody></table></div>{editing&&<div className="modal-panel"><form className="form-card" onSubmit={save}><div className="section-title"><div><h2>Edit {editing.username}</h2><p>{editing.email}</p></div><button className="secondary-button" type="button" onClick={()=>setEditing(null)}>Close</button></div><label>Username<input required minLength="3" maxLength="30" pattern="[A-Za-z0-9._-]+" value={editing.username} onChange={e=>setEditing({...editing,username:e.target.value})}/></label><label>Role<select value={editing.role} onChange={e=>setEditing({...editing,role:e.target.value})}><option>Student</option><option>Teacher</option><option>Admin</option></select></label><label>New password (optional)<input type="password" minLength="8" value={editing.newPassword} onChange={e=>setEditing({...editing,newPassword:e.target.value})} placeholder="Leave blank to keep current password"/></label><button className="primary-button">Save changes</button></form></div>}</>
}
