const MEDALS = ['🥇', '🥈', '🥉']

export default function TopThree({ rows }) {
  return (
    <aside className="top-three">
      <span className="eyebrow">CURRENT TOP 3</span>
      {rows.slice(0, 3).map((row, i) => (
        <div className={`top-row place-${i + 1}`} key={row.userId}>
          <span>{MEDALS[i]}</span>
          <div>
            <strong>{row.displayName}</strong>
            <small>{row.correctAnswers} correct</small>
          </div>
          <b>{row.score}</b>
        </div>
      ))}
    </aside>
  )
}