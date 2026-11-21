import { useState } from 'react';
import { platformApi } from '../../api/platformApi';
import DataTable from '../../components/DataTable';
import EntityForm from '../../components/EntityForm';
import { Badge, ErrorBanner, Modal, Notice, PageHeader, Spinner } from '../../components/ui';
import { useAsync } from '../../hooks/useAsync';

const profileFields = [
  { name: 'name', label: 'College name', required: true, full: true },
  { name: 'contactEmail', label: 'Contact email', type: 'email' },
  { name: 'contactPhone', label: 'Contact phone' },
  { name: 'address', label: 'Address', type: 'textarea' },
  { name: 'plan', label: 'Plan', type: 'select', required: true,
    options: ['basic', 'standard', 'premium', 'enterprise'].map((v) => ({ value: v, label: v })) },
  { name: 'maxStudents', label: 'Max students', type: 'number', min: 1, hint: 'Leave blank for unlimited.' },
];

const createFields = [
  { name: 'code', label: 'College code', required: true, placeholder: 'stxavier',
    hint: 'Lowercase letters, digits, underscores. Used in URLs and as the database name.' },
  ...profileFields,
  { name: 'adminFullName', label: 'First admin — full name', required: true },
  { name: 'adminEmail', label: 'First admin — email', type: 'email', required: true },
  { name: 'adminPassword', label: 'First admin — password', type: 'password', required: true, hint: 'At least 8 characters.' },
];

export default function CollegesPage() {
  const [search, setSearch] = useState('');
  const [modal, setModal] = useState(null); // {mode:'create'|'edit'|'delete'|'stats', college}
  const [notice, setNotice] = useState('');
  const [actionError, setActionError] = useState(null);
  const { data, error, loading, reload } = useAsync(() => platformApi.listColleges({ search, take: 200 }), [search]);

  async function run(fn, message) {
    setActionError(null);
    try {
      await fn();
      setNotice(message);
      reload();
    } catch (e) {
      setActionError(e);
    }
  }

  const columns = [
    { key: 'code', label: 'Code', render: (c) => <code>{c.code}</code> },
    { key: 'name', label: 'Name' },
    { key: 'plan', label: 'Plan' },
    { key: 'maxStudents', label: 'Student limit', render: (c) => c.maxStudents ?? 'Unlimited' },
    { key: 'databaseName', label: 'Database', render: (c) => <code>{c.databaseName}</code> },
    { key: 'status', label: 'Status',
      render: (c) => <Badge tone={c.status === 'active' ? 'ok' : 'warn'}>{c.status}</Badge> },
    { key: 'portal', label: 'Portal', render: (c) => <a href={`/c/${c.code}/login`} target="_blank" rel="noreferrer">Open ↗</a> },
  ];

  return (
    <>
      <PageHeader title="Colleges" subtitle="Each college runs on its own isolated database.">
        <input className="search" type="search" placeholder="Search colleges…"
          onKeyDown={(e) => e.key === 'Enter' && setSearch(e.currentTarget.value)} />
        <button className="btn" onClick={() => run(async () => {
          const r = await platformApi.migrateAll();
          if (r.failures.length) throw new Error(`Migration failures: ${r.failures.join('; ')}`);
        }, 'All college databases are on the latest schema.')}>Upgrade all DBs</button>
        <button className="btn btn-primary" onClick={() => setModal({ mode: 'create' })}>+ New college</button>
      </PageHeader>
      <Notice>{notice}</Notice>
      <ErrorBanner error={error || actionError} />
      {loading && !data ? <Spinner /> : (
        <DataTable columns={columns} rows={data?.items} empty="No colleges yet — create the first one."
          actions={(c) => (
            <>
              <button className="btn btn-small" onClick={() => setModal({ mode: 'stats', college: c })}>Stats</button>
              <button className="btn btn-small" onClick={() => setModal({ mode: 'edit', college: c })}>Edit</button>
              {c.status === 'active'
                ? <button className="btn btn-small" onClick={() => run(() => platformApi.suspend(c.id), `${c.name} suspended.`)}>Suspend</button>
                : <button className="btn btn-small" onClick={() => run(() => platformApi.activate(c.id), `${c.name} activated.`)}>Activate</button>}
              <button className="btn btn-small btn-danger" onClick={() => setModal({ mode: 'delete', college: c })}>Delete</button>
            </>
          )} />
      )}

      {modal?.mode === 'create' && (
        <Modal title="Onboard a new college" onClose={() => setModal(null)}>
          <EntityForm fields={createFields} initial={{ plan: 'standard' }} submitLabel="Create college & database"
            onCancel={() => setModal(null)}
            onSubmit={async (v) => {
              const c = await platformApi.createCollege(v);
              setModal(null);
              setNotice(`Created ${c.name} with database "${c.databaseName}". Portal: /c/${c.code}/login`);
              reload();
            }} />
        </Modal>
      )}
      {modal?.mode === 'edit' && (
        <Modal title={`Edit ${modal.college.name}`} onClose={() => setModal(null)}>
          <EntityForm fields={profileFields} initial={modal.college} onCancel={() => setModal(null)}
            onSubmit={async (v) => {
              await platformApi.updateCollege(modal.college.id, v);
              setModal(null);
              setNotice('College updated.');
              reload();
            }} />
        </Modal>
      )}
      {modal?.mode === 'delete' && (
        <DeleteCollege college={modal.college} onClose={() => setModal(null)}
          onDone={(msg) => { setModal(null); setNotice(msg); reload(); }} />
      )}
      {modal?.mode === 'stats' && <StatsModal college={modal.college} onClose={() => setModal(null)} />}
    </>
  );
}

function DeleteCollege({ college, onClose, onDone }) {
  return (
    <Modal title={`Delete ${college.name}?`} onClose={onClose}>
      <p>This removes the college from the platform.</p>
      <EntityForm submitLabel="Delete college" onCancel={onClose}
        fields={[{ name: 'dropDatabase', label: `Also permanently delete database "${college.databaseName}" (cannot be undone)`, type: 'checkbox', full: true }]}
        onSubmit={async ({ dropDatabase }) => {
          await platformApi.deleteCollege(college.id, dropDatabase);
          onDone(`${college.name} deleted${dropDatabase ? ' and its database dropped' : ' (database kept)'}.`);
        }} />
    </Modal>
  );
}

function StatsModal({ college, onClose }) {
  const { data, error } = useAsync(() => platformApi.stats(college.id), [college.id]);
  return (
    <Modal title={`${college.name} — usage`} onClose={onClose}>
      <ErrorBanner error={error} />
      {!data ? <Spinner /> : (
        <div className="stats-grid">
          {['students', 'faculty', 'courses', 'departments', 'users'].map((k) => (
            <div key={k} className="card stat"><div className="stat-label">{k}</div><div className="stat-value">{data[k]}</div></div>
          ))}
        </div>
      )}
    </Modal>
  );
}
