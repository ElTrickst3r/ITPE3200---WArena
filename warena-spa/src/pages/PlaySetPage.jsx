import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../services/api'

export default function PlaySetPage(){
 const {id}=useParams();const [set,setSet]=useState(null),[index,setIndex]=useState(0),[answer,setAnswer]=useState(''),[result,setResult]=useState(null),[error,setError]=useState('')
 useEffect(()=>{api.get(`/api/challenge-sets/${id}`).then(setSet).catch(e=>setError(e.message))},[id])
 if(error)return <div className="error-box">{error}</div>;if(!set)return <p>Loading…</p>;if(!set.questions.length)return <div className="empty"><h2>No questions in this set</h2></div>
 const q=set.questions[index]
 async function submit(e){e.preventDefault();try{setResult(await api.post(`/api/progress/questions/${q.id}/answer`,{answer}))}catch(e){setError(e.message)}}
 function next(){if(index+1<set.questions.length){setIndex(index+1);setAnswer('');setResult(null)}else{setIndex(set.questions.length);setResult(null)}}
 if(index>=set.questions.length)return <div className="finish-card"><span className="podium-medal">🏁</span><h1>{set.name} complete</h1><p>You reached the end of the set. Your answers are saved in History.</p><div className="actions"><Link className="primary-button" to="/sets">More challenge sets</Link><Link className="secondary-button" to="/history">View history</Link></div></div>
 return <div className="play-layout"><div className="play-progress"><div><strong>{set.name}</strong><span>Question {index+1} of {set.questions.length}</span></div><div className="progress-track"><span style={{width:`${((index+1)/set.questions.length)*100}%`}}/></div></div><article className="play-card"><div className="card-top"><span className="pill">{q.category}</span><span className={`difficulty ${q.difficulty.toLowerCase()}`}>{q.difficulty}</span></div><h1>{q.title}</h1><p className="question-text">{q.prompt}</p>{result?<div className={`result-box ${result.isCorrect?'success':'failure'}`}><strong>{result.isCorrect?'Correct!':'Not quite'}</strong><span>{result.isCorrect?`+${result.pointsEarned} XP`:'0 XP'}</span><button className="primary-button" onClick={next}>{index+1<set.questions.length?'Next question':'Finish set'}</button></div>:<form onSubmit={submit}>{q.type==='MultipleChoice'?<div className="option-list">{q.options.map(o=><label className="option" key={o}><input required type="radio" value={o} checked={answer===o} onChange={e=>setAnswer(e.target.value)}/><span>{o}</span></label>)}</div>:<label>Your answer<input required value={answer} onChange={e=>setAnswer(e.target.value)}/></label>}<div className="submit-row"><span>{q.points} XP</span><button className="primary-button">Submit</button></div></form>}</article></div>
}
