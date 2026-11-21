import { useState } from 'react';
import { useAsync } from '../hooks/useAsync';
import DataTable from './DataTable';
import EntityForm from './EntityForm';
import { ErrorBanner, Modal, PageHeader, Spinner } from './ui';

const PAGE = 25;

/**
 * Generic list + search + create/edit/delete screen for any REST resource.
 * New entity screens are added by configuration, not by copying code.
 */
export default function ResourcePage({
  title, subtitle, resource, columns, fields, editFields, canWrite, filters, toInitial = (r) => r,
  extraActions,
}) {
  const [search, setSearch] = useState('');
  const [skip, setSkip] = useState(0);
  const [editing, setEditing] = useState(null); // null | {} (new) | row
  const [actionError, setActionError] = useState(null);

  const { data, error, loading, reload } = useAsync(
    () => resource.list({ skip, take: PAGE, search, ...filters }),
    [resource, skip, search, JSON.stringify(filters ?? {})],
  );

  async function save(values) {
    if (editing?.id) await resource.update(editing.id, values);
    else await resource.create(values);
    setEditing(null);
    reload();
  }

  async function remove(row) {
    if (!confirm('Delete this record?')) return;
    setActionError(null);
    try {
      await resource.remove(row.id);
      reload();
    } catch (e) {
      setActionError(e);
    }
  }

  const total = data?.total ?? 0;
  return (
    <>
      <PageHeader title={title} subtitle={subtitle}>
        <input className="search" type="search" placeholder="Search…" defaultValue={search}
          onKeyDown={(e) => e.key === 'Enter' && (setSkip(0), setSearch(e.currentTarget.value))} />
        {canWrite && <button className="btn btn-primary" onClick={() => setEditing({})}>+ Add</button>}
      </PageHeader>
      <ErrorBanner error={error || actionError} />
      {loading && !data ? <Spinner /> : (
        <DataTable columns={columns} rows={data?.items}
          actions={canWrite || extraActions ? (row) => (
            <>
              {extraActions?.(row)}
              {canWrite && <button className="btn btn-small" onClick={() => setEditing(row)}>Edit</button>}
              {canWrite && <button className="btn btn-small btn-danger" onClick={() => remove(row)}>Delete</button>}
            </>
          ) : undefined} />
      )}
      {total > PAGE && (
        <div className="pager">
          <button className="btn btn-small" disabled={skip === 0} onClick={() => setSkip(skip - PAGE)}>← Prev</button>
          <span className="muted">{skip + 1}–{Math.min(skip + PAGE, total)} of {total}</span>
          <button className="btn btn-small" disabled={skip + PAGE >= total} onClick={() => setSkip(skip + PAGE)}>Next →</button>
        </div>
      )}
      {editing && (
        <Modal title={editing.id ? `Edit ${title.replace(/s$/, '')}` : `New ${title.replace(/s$/, '')}`} onClose={() => setEditing(null)}>
          <EntityForm
            fields={editing.id && editFields ? editFields : fields}
            initial={editing.id ? toInitial(editing) : {}}
            onSubmit={save}
            onCancel={() => setEditing(null)} />
        </Modal>
      )}
    </>
  );
}
