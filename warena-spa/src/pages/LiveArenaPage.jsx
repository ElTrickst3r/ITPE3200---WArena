import { useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { api, getToken } from '../services/api'
import { useAuth } from '../context/AuthContext'

const themeIcon = { cars: '🏎️', horses: '🏇', rockets: '🚀', boats: '🚤' }
const flippedThemes = new Set(['cars','horses','boats'])

export default function LiveArenaPage(){
  const { user, canManage } = useAuth()
  const [sets,setSets]=useState([]),[room,setRoom]=useState(null),[code,setCode]=useState('')
  const [create,setCreate]=useState({challengeSetId:'',theme:'random',timeLimitSeconds:20,autoContinue:true,autoContinueDelaySeconds:3})
  const [answer,setAnswer]=useState(''),[answerInfo,setAnswerInfo]=useState(null),[error,setError]=useState(''),[remaining,setRemaining]=useState(0)
  const [roomAutomation,setRoomAutomation]=useState({enabled:true,delaySeconds:3})
  const questionKey=room?.currentQuestion?.id
  const startedRef=useRef(Date.now())

  useEffect(()=>{ if(canManage) api.get('/api/challenge-sets?isPublished=true').then(setSets).catch(e=>setError(e.message)) },[canManage])

  useEffect(()=>{
    if(!room?.code) return
    const connection=new signalR.HubConnectionBuilder().withUrl('/hubs/live',{accessTokenFactory:()=>getToken()||''}).withAutomaticReconnect().build()
    connection.on('StateChanged',state=>setRoom(state))
    connection.start().then(()=>connection.invoke('JoinRoom',room.code)).catch(e=>setError(e.message))
    return ()=>{ connection.stop() }
  },[room?.code])

  useEffect(()=>{
    if(!room?.currentQuestion){setRemaining(0);return}
    startedRef.current=Date.now(); setAnswer(''); setAnswerInfo(null); setRemaining(room.currentQuestion.timeLimitSeconds)
    const timer=setInterval(()=>setRemaining(value=>Math.max(0,value-.1)),100)
    return ()=>clearInterval(timer)
  },[questionKey])

  async function createRoom(e){
    e.preventDefault();setError('')
    try{
      const state=await api.post('/api/live/rooms',{challengeSetId:Number(create.challengeSetId),theme:create.theme,timeLimitSeconds:Number(create.timeLimitSeconds)})
      setRoomAutomation({enabled:create.autoContinue,delaySeconds:Number(create.autoContinueDelaySeconds)})
      setRoom(state);setCode(state.code)
    }catch(e){setError(e.message)}
  }
  async function joinRoom(e){e.preventDefault();setError('');try{const state=await api.post(`/api/live/rooms/${code.trim().toUpperCase()}/join`,{});setRoomAutomation({enabled:false,delaySeconds:3});setRoom(state);setCode(state.code)}catch(e){setError(e.message)}}
  async function start(){try{setRoom(await api.post(`/api/live/rooms/${room.code}/start`,{}))}catch(e){setError(e.message)}}
  async function next(){try{setRoom(await api.post(`/api/live/rooms/${room.code}/next`,{}))}catch(e){setError(e.message)}}
  async function submit(e){e.preventDefault();if(!answer||remaining<=0)return;try{const elapsedMs=Math.max(0,Date.now()-startedRef.current);const result=await api.post(`/api/live/rooms/${room.code}/answer`,{answer,elapsedMs});setAnswerInfo({score:result.score});setRoom(result.state)}catch(e){setError(e.message)}}

  if(room) return <RoomView room={room} user={user} canManage={canManage} automation={roomAutomation} answer={answer} setAnswer={setAnswer} answerInfo={answerInfo} remaining={remaining} onSubmit={submit} onStart={start} onNext={next} onLeave={()=>{setRoom(null);setCode('')}} error={error}/>

  return <><div className="section-title"><div><h1>Live Arena</h1><p>Join a room and compete in real time. Correctness comes first; faster correct answers earn a bigger time bonus.</p></div></div>{error&&<div className="error-box">{error}</div>}<div className="live-entry-grid"><section className="entry-card"><span className="live-icon">🎮</span><h2>Join a room</h2><p>Enter the five-character room code from your teacher.</p><form onSubmit={joinRoom}><label>Room code<input required maxLength="5" value={code} onChange={e=>setCode(e.target.value.toUpperCase())} placeholder="WA42X"/></label><button className="primary-button">Join Arena</button></form></section>{canManage&&<section className="entry-card"><span className="live-icon">🏁</span><h2>Create a room</h2><p>Use an existing challenge set and choose a race theme.</p><form onSubmit={createRoom}><label>Challenge set<select required value={create.challengeSetId} onChange={e=>setCreate({...create,challengeSetId:e.target.value})}><option value="">Choose a set</option>{sets.map(s=><option key={s.id} value={s.id}>{s.name} ({s.questionCount})</option>)}</select></label><div className="two"><label>Theme<select value={create.theme} onChange={e=>setCreate({...create,theme:e.target.value})}><option value="random">Random</option><option value="cars">Cars</option><option value="horses">Horses</option><option value="rockets">Rockets</option><option value="boats">Boats</option></select></label><label>Seconds/question<input type="number" min="5" max="60" value={create.timeLimitSeconds} onChange={e=>setCreate({...create,timeLimitSeconds:e.target.value})}/></label></div><label className="check-row"><input type="checkbox" checked={create.autoContinue} onChange={e=>setCreate({...create,autoContinue:e.target.checked})}/><span><strong>Auto-continue</strong><small>Move on when everyone has answered or time runs out.</small></span></label>{create.autoContinue&&<label>Seconds before next question<input type="number" min="1" max="10" value={create.autoContinueDelaySeconds} onChange={e=>setCreate({...create,autoContinueDelaySeconds:e.target.value})}/></label>}<button className="primary-button">Create room</button></form></section>}</div></>
}

function RoomView({room,user,canManage,automation,answer,setAnswer,answerInfo,remaining,onSubmit,onStart,onNext,onLeave,error}){
 const player=room.leaderboard.find(x=>x.userId===user?.userId)
 const joined=Boolean(player)
 const isHost=room.hostUserId===user?.userId
 const autoTimerRef=useRef(null)
 const scheduledQuestionRef=useRef(null)
 const questionId=room.currentQuestion?.id
 const everyoneAnswered=room.participantCount>0&&room.currentAnswerCount>=room.participantCount
 const roundReady=remaining<=0||everyoneAnswered

 useEffect(()=>{
   if(scheduledQuestionRef.current!==questionId){
     if(autoTimerRef.current) clearTimeout(autoTimerRef.current)
     autoTimerRef.current=null
     scheduledQuestionRef.current=null
   }
 },[questionId])

 useEffect(()=>{
   const enabled=isHost&&automation.enabled&&room.status==='Running'&&Boolean(room.currentQuestion)
   if(!enabled||!roundReady){
     if(autoTimerRef.current&&scheduledQuestionRef.current===questionId){clearTimeout(autoTimerRef.current);autoTimerRef.current=null;scheduledQuestionRef.current=null}
     return
   }
   if(scheduledQuestionRef.current===questionId)return
   scheduledQuestionRef.current=questionId
   autoTimerRef.current=setTimeout(()=>{autoTimerRef.current=null;onNext()},Math.max(1,automation.delaySeconds)*1000)
 },[isHost,automation.enabled,automation.delaySeconds,room.status,questionId,roundReady,onNext,room.currentQuestion])

 useEffect(()=>()=>{if(autoTimerRef.current)clearTimeout(autoTimerRef.current)},[])

 function manualNext(){if(autoTimerRef.current)clearTimeout(autoTimerRef.current);autoTimerRef.current=null;scheduledQuestionRef.current=questionId;onNext()}

 if(room.status==='Completed') return <FinalPodium room={room} onLeave={onLeave}/>
 return <><div className="live-header"><div><span className="eyebrow">LIVE ARENA · {room.theme.toUpperCase()}</span><h1>{room.challengeSetName}</h1><p>Room <strong>{room.code}</strong> · {room.leaderboard.length} player{room.leaderboard.length===1?'':'s'}{isHost&&automation.enabled?` · Auto-continue ${automation.delaySeconds}s`:''}</p></div><div className="live-actions">{isHost&&room.status==='Waiting'&&<button className="primary-button" onClick={onStart} disabled={room.leaderboard.length===0}>Start game</button>}{isHost&&room.status==='Running'&&<button className="primary-button" onClick={manualNext}>Next question</button>}<button className="secondary-button" onClick={onLeave}>Leave</button></div></div>{error&&<div className="error-box">{error}</div>}
 <RaceBoard room={room}/>
 {room.status==='Waiting'&&<div className="waiting-card"><span>⏳</span><h2>Waiting for the host</h2>{!joined&&!canManage?<p>You need to join the room again.</p>:<p>The race begins when the teacher starts the game.</p>}</div>}
 {room.status==='Running'&&room.currentQuestion&&<div className="live-play"><article className="live-question"><div className="question-status"><span>Question {room.currentQuestion.index+1} / {room.currentQuestion.total}</span><strong className={remaining<5?'urgent':''}>{remaining.toFixed(1)}s</strong></div><h2>{room.currentQuestion.title}</h2><p className="question-text">{room.currentQuestion.prompt}</p>{isHost&&!joined?<div className="host-note">Host view: students answer on their own devices.{automation.enabled&&roundReady?` Advancing automatically in ${automation.delaySeconds} seconds.`:''}</div>:answerInfo?<div className="result-box success"><strong>Answer submitted</strong><span>+{answerInfo.score} live points</span></div>:<form onSubmit={onSubmit}>{room.currentQuestion.type==='MultipleChoice'?<div className="option-list">{room.currentQuestion.options.map(o=><label className="option" key={o}><input required type="radio" value={o} checked={answer===o} onChange={e=>setAnswer(e.target.value)}/><span>{o}</span></label>)}</div>:<label>Your answer<input required value={answer} onChange={e=>setAnswer(e.target.value)}/></label>}<button className="primary-button" disabled={remaining<=0}>{remaining<=0?'Time is up':'Lock in answer'}</button></form>}</article><TopThree rows={room.leaderboard}/></div>}
 </>
}

function RaceBoard({room}){
 const max=Math.max(1,room.maxPossibleScore||((room.currentQuestion?.total||1)*1000))
 const icon=themeIcon[room.theme]||'🏎️'
 const flipped=flippedThemes.has(room.theme)
 const lanes=[...room.leaderboard].sort((a,b)=>(a.lane??a.rank)-(b.lane??b.rank)).slice(0,8)
 return <section className={`race-board ${room.theme}`}><div className="finish-line" aria-hidden="true"></div>{lanes.map(row=>{const pct=room.status==='Waiting'?2:Math.min(94,Math.max(2,(row.score/max)*94));return <div className="race-lane" key={row.userId}><span className="lane-name">#{row.rank} {row.displayName}</span><div className="track"><span className={`racer${flipped?' flipped':''}`} style={{left:`${pct}%`}}>{icon}</span></div></div>})}{room.leaderboard.length===0&&<div className="race-empty">Players will appear on the track when they join.</div>}</section>
}

function TopThree({rows}){return <aside className="top-three"><span className="eyebrow">CURRENT TOP 3</span>{rows.slice(0,3).map((r,i)=><div className={`top-row place-${i+1}`} key={r.userId}><span>{['🥇','🥈','🥉'][i]}</span><div><strong>{r.displayName}</strong><small>{r.correctAnswers} correct</small></div><b>{r.score}</b></div>)}</aside>}

function FinalPodium({room,onLeave}){
 const top=room.leaderboard.slice(0,3);const first=top[0],second=top[1],third=top[2]
 return <div className="finale"><span className="eyebrow">FINAL RESULTS</span><h1>{room.challengeSetName}</h1><p>The podium is decided by total score, then correct answers, then total response time.</p><div className="podium">{second&&<PodiumPlace row={second} place={2}/>} {first&&<PodiumPlace row={first} place={1}/>} {third&&<PodiumPlace row={third} place={3}/>}</div><button className="primary-button" onClick={onLeave}>Back to Live Arena</button></div>
}
function PodiumPlace({row,place}){return <div className={`podium-place p${place}`}><div className="winner"><span>{['','🥇','🥈','🥉'][place]}</span><strong>{row.displayName}</strong><small>{row.score} pts</small></div><div className="podium-block">{place}</div></div>}
