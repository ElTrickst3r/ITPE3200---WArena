const THEME_ICONS = { cars: '🏎️', horses: '🏇', rockets: '🚀', boats: '🚤' }
// These emojis face left by default, so they are mirrored to race towards the finish line.
const FLIPPED_THEMES = new Set(['cars', 'horses', 'boats'])
const MAX_LANES = 8

export default function RaceBoard({ room }) {
  const max = Math.max(1, room.maxPossibleScore || ((room.currentQuestion?.total || 1) * 1000))
  const icon = THEME_ICONS[room.theme] || '🏎️'
  const flipped = FLIPPED_THEMES.has(room.theme)
  const lanes = [...room.leaderboard]
    .sort((a, b) => (a.lane ?? a.rank) - (b.lane ?? b.rank))
    .slice(0, MAX_LANES)

  // Position on the track in percent, kept between 2 and 94 so the racer stays inside the lane.
  const positionOf = row => room.status === 'Waiting' ? 2 : Math.min(94, Math.max(2, (row.score / max) * 94))

  return (
    <section className={`race-board ${room.theme}`}>
      <div className="finish-line" aria-hidden="true"></div>
      {lanes.map(row => (
        <div className="race-lane" key={row.userId}>
          <span className="lane-name">#{row.rank} {row.displayName}</span>
          <div className="track">
            <span className={`racer${flipped ? ' flipped' : ''}`} style={{ left: `${positionOf(row)}%` }}>{icon}</span>
          </div>
        </div>
      ))}
      {room.leaderboard.length === 0 && <div className="race-empty">Players will appear on the track when they join.</div>}
    </section>
  )
}