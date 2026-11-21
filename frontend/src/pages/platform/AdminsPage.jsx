import { useState } from 'react';
import { platformApi } from '../../api/platformApi';
import DataTable from '../../components/DataTable';
import EntityForm from '../../components/EntityForm';
import { ErrorBanner, Modal, PageHeader, Spinner } from '../../components/ui';
import { useAsync } from '../../hooks/useAsync';

export default function AdminsPage() {
  const [open, setOpen] = useState(false);
  const { data, error, loading, reload } = useAsync(() => platformApi.listAdmins(), []);
  return (
    <>
      <PageHeader title="Platform admins" subtitle="People who can onboard and manage colleges.">
        <button className="btn btn-primary" onClick={() => setOpen(true)}>+ Add admin</button>
      </PageHeader>
      <ErrorBanner error={error} />
      {loading && !data ? <Spinner /> : (
        <DataTable rows={data} columns={[
          { key: 'email', label: 'Email' },
          { key: 'fullName', label: 'Name' },
          { key: 'createdAt', label: 'Created', render: (a) => new Date(a.createdAt).toLocaleDateString() },
        ]} />
      )}
      {open && (
        <Modal title="New platform admin" onClose={() => setOpen(false)}>
          <EntityForm onCancel={() => setOpen(false)}
            fields={[
              { name: 'fullName', label: 'Full name', required: true },
              { name: 'email', label: 'Email', type: 'email', required: true },
              { name: 'password', label: 'Password', type: 'password', required: true },
            ]}
            onSubmit={async (v) => { await platformApi.createAdmin(v); setOpen(false); reload(); }} />
        </Modal>
      )}
    </>
  );
}
