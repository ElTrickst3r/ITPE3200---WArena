import { useState } from 'react'

const DEFAULT_FORM = { challengeSetId: '', theme: 'random', timeLimitSeconds: 20, autoContinue: true, autoContinueDelaySeconds: 3 }

export default function CreateRoomCard({ sets, onCreate }) {
  const [form, setForm] = useState(DEFAULT_FORM)

  const update = field => e => setForm({ ...form, [field]: e.target.value })

  function submit(e) {
    e.preventDefault()
    onCreate(form)
  }

  return (
    <section className="entry-card">
      <span className="live-icon">🏁</span>
      <h2>Create a room</h2>
      <p>Use an existing challenge set and choose a race theme.</p>
      <form onSubmit={submit}>
        <label>
          Challenge set
          <select required value={form.challengeSetId} onChange={update('challengeSetId')}>
            <option value="">Choose a set</option>
            {sets.map(s => <option key={s.id} value={s.id}>{s.name} ({s.questionCount})</option>)}
          </select>
        </label>
        <div className="two">
          <label>
            Theme
            <select value={form.theme} onChange={update('theme')}>
              <option value="random">Random</option>
              <option value="cars">Cars</option>
              <option value="horses">Horses</option>
              <option value="rockets">Rockets</option>
              <option value="boats">Boats</option>
            </select>
          </label>
          <label>
            Seconds/question
            <input type="number" min="5" max="60" value={form.timeLimitSeconds} onChange={update('timeLimitSeconds')} />
          </label>
        </div>
        <label className="check-row">
          <input type="checkbox" checked={form.autoContinue} onChange={e => setForm({ ...form, autoContinue: e.target.checked })} />
          <span>
            <strong>Auto-continue</strong>
            <small>Move on when everyone has answered or time runs out.</small>
          </span>
        </label>
        {form.autoContinue && (
          <label>
            Seconds before next question
            <input type="number" min="1" max="10" value={form.autoContinueDelaySeconds} onChange={update('autoContinueDelaySeconds')} />
          </label>
        )}
        <button className="primary-button">Create room</button>
      </form>
    </section>
  )
}