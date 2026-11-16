const MEDALS = { 1: '🥇', 2: '🥈', 3: '🥉' }

export default function FinalPodium({ room, onLeave }) {
  const [first, second, third] = room.leaderboard

  return (
    <div className="finale">
      <span className="eyebrow">FINAL RESULTS</span>
      <h1>{room.challengeSetName}</h1>
      <p>The podium is decided by total score, then correct answers, then total response time.</p>
      {/* Second place is rendered first so the winner stands in the middle of the podium. */}
      <div className="podium">
        {second && <PodiumPlace row={second} place={2} />}
        {first && <PodiumPlace row={first} place={1} />}
        {third && <PodiumPlace row={third} place={3} />}
      </div>
      <button type="button" className="primary-button" onClick={onLeave}>Back to Live Arena</button>
    </div>
  )
}

function PodiumPlace({ row, place }) {
  return (
    <div className={`podium-place p${place}`}>
      <div className="winner">
        <span>{MEDALS[place]}</span>
        <strong>{row.displayName}</strong>
        <small>{row.score} pts</small>
      </div>
      <div className="podium-block">{place}</div>
    </div>
  )
}