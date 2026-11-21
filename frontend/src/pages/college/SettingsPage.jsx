import { useState } from 'react';
import DataTable from '../../components/DataTable';
import EntityForm from '../../components/EntityForm';
import { ErrorBanner, Modal, PageHeader, Spinner } from '../../components/ui';
import { useAsync } from '../../hooks/useAsync';
import { useCollege } from './CollegeContext';

/** Free-form per-college configuration: lets each college customise without code changes. */
export default function SettingsPage() {
  const { api } = useCollege();
  const [editing, setEditing] = useState(null);
  const { data, error, reload } = useAsync(() => api.settings.get(), [api]);
  const rows = Object.entries(data ?? {}).map(([key, value]) => ({ id: key, key, value }));

  return (
    <>
      <PageHeader title="Settings" subtitle="College-specific configuration (academic year, grading, branding…).">
        <button className="btn btn-primary" onClick={() => setEditing({})}>+ Add setting</button>
      </PageHeader>
      <ErrorBanner error={error} />
      {!data ? !error && <Spinner /> : (
        <DataTable rows={rows} empty="No settings yet. Try academicYear = &quot;2026-27&quot; or passPercentage = 40."
          columns={[
            { key: 'key', label: 'Key', render: (r) => <code>{r.key}</code> },
            { key: 'value', label: 'Value', render: (r) => <code>{JSON.stringify(r.value)}</code> },
          ]}
          actions={(r) => <button className="btn btn-small" onClick={() => setEditing(r)}>Edit</button>} />
      )}
      {editing && (
        <Modal title={editing.key ? `Edit ${editing.key}` : 'New setting'} onClose={() => setEditing(null)}>
          <EntityForm onCancel={() => setEditing(null)}
            initial={editing.key ? { key: editing.key, value: JSON.stringify(editing.value) } : {}}
            fields={[
              { name: 'key', label: 'Key', required: true },
              { name: 'value', label: 'Value (JSON or plain text)', required: true, full: true },
            ]}
            onSubmit={async ({ key, value }) => {
              let parsed;
              try { parsed = JSON.parse(value); } catch { parsed = value; }
              await api.settings.save({ [key]: parsed });
              setEditing(null);
              reload();
            }} />
        </Modal>
      )}
    </>
  );
}
