import { useState } from 'react'

export default function JoinRoomCard({ onJoin }) {
  const [code, setCode] = useState('')

  function submit(e) {
    e.preventDefault()
    onJoin(code.trim().toUpperCase())
  }

  return (
    <section className="entry-card">
      <span className="live-icon">🎮</span>
      <h2>Join a room</h2>
      <p>Enter the five-character room code from your teacher.</p>
      <form onSubmit={submit}>
        <label>
          Room code
          <input required minLength="5" maxLength="5" value={code} onChange={e => setCode(e.target.value.toUpperCase())} placeholder="WA42X" />
        </label>
        <button className="primary-button">Join Arena</button>
      </form>
    </section>
  )
}