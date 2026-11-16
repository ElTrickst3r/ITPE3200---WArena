// Shared list with edit and delete buttons, used by all three management tabs.
export default function ManageList({ title, items, emptyText, renderInfo, onEdit, onDelete }) {
  return (
    <section className="manage-list">
      <h2>{title}</h2>
      {items.length === 0 ? (
        <p>{emptyText}</p>
      ) : (
        items.map(item => (
          <div className="manage-row" key={item.id}>
            <div>{renderInfo(item)}</div>
            <div>
              <button type="button" onClick={() => onEdit(item.id)}>Edit</button>
              <button type="button" className="danger" onClick={() => onDelete(item.id)}>Delete</button>
            </div>
          </div>
        ))
      )}
    </section>
  )
}