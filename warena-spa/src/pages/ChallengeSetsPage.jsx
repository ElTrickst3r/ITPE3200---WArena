import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../services/api'

export default function ChallengeSetsPage(){
 const [sets,setSets]=useState([]),[filters,setFilters]=useState({search:'',minQuestions:''}),[error,setError]=useState('')
 async function load(){try{const p=new URLSearchParams({isPublished:'true'});if(filters.search)p.set('search',filters.search);if(filters.minQuestions)p.set('minQuestions',filters.minQuestions);setSets(await api.get(`/api/challenge-sets?${p}`))}catch(e){setError(e.message)}}
 useEffect(()=>{load()},[])
 return <><div className="section-title"><div><h1>Challenge sets</h1><p>Choose a named collection such as HTML Basics or CSS Fundamentals.</p></div></div><form className="filter-panel compact" onSubmit={e=>{e.preventDefault();load()}}><label>Search<input value={filters.search} onChange={e=>setFilters({...filters,search:e.target.value})}/></label><label>Minimum questions<input type="number" min="1" value={filters.minQuestions} onChange={e=>setFilters({...filters,minQuestions:e.target.value})}/></label><button className="primary-button">Filter</button></form>{error&&<div className="error-box">{error}</div>}<div className="card-grid">{sets.map(s=><article className="card" key={s.id}><span className="pill">{s.questionCount} questions</span><h3>{s.name}</h3><p>{s.description}</p><div className="card-meta"><span>{s.possibleXp} possible XP</span></div><Link className="primary-button" to={`/sets/${s.id}/play`}>Open set</Link></article>)}</div></>
}
