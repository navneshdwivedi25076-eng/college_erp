/**
 * Generic table. columns: [{ key, label, render?(row) }], actions?(row) -> ReactNode
 */
export default function DataTable({ columns, rows, actions, empty = 'No records yet.' }) {
  if (!rows?.length) return <div className="empty">{empty}</div>;
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            {columns.map((c) => <th key={c.key}>{c.label}</th>)}
            {actions && <th className="actions-col" aria-label="Actions" />}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={row.id ?? i}>
              {columns.map((c) => <td key={c.key}>{c.render ? c.render(row) : row[c.key] ?? '—'}</td>)}
              {actions && <td className="actions-col">{actions(row)}</td>}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
