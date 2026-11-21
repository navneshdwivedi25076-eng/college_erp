import { useState } from 'react';
import DataTable from '../../components/DataTable';
import EntityForm from '../../components/EntityForm';
import { Badge, ErrorBanner, Modal, Notice, PageHeader, Spinner, money } from '../../components/ui';
import { useAsync } from '../../hooks/useAsync';
import { toOptions, useLookups } from '../../hooks/useLookups';
import { useCollege } from './CollegeContext';

const TONE = { paid: 'ok', partial: 'info', unpaid: 'warn' };

export default function FeesPage() {
  const { api } = useCollege();
  const lk = useLookups(api, ['students']);
  const [status, setStatus] = useState('');
  const [search, setSearch] = useState('');
  const [modal, setModal] = useState(null);
  const [notice, setNotice] = useState('');
  const [actionError, setActionError] = useState(null);
  const { data, error, loading, reload } = useAsync(() => api.fees.list({ status, search, take: 200 }), [api, status, search]);

  async function remove(inv) {
    if (!confirm('Delete this invoice?')) return;
    setActionError(null);
    try { await api.fees.remove(inv.id); reload(); } catch (e) { setActionError(e); }
  }

  return (
    <>
      <PageHeader title="Fees" subtitle="Bill students individually or a whole semester at once.">
        <input className="search" type="search" placeholder="Search student / description…"
          onKeyDown={(e) => e.key === 'Enter' && setSearch(e.currentTarget.value)} />
        <select value={status} onChange={(e) => setStatus(e.target.value)} aria-label="Status">
          <option value="">All statuses</option>
          <option value="unpaid">Unpaid</option>
          <option value="partial">Partial</option>
          <option value="paid">Paid</option>
        </select>
        <button className="btn btn-primary" onClick={() => setModal({ mode: 'invoice' })}>+ New invoice</button>
      </PageHeader>
      <Notice>{notice}</Notice>
      <ErrorBanner error={error || actionError} />
      {loading && !data ? <Spinner /> : (
        <DataTable rows={data?.items} columns={[
          { key: 'rollNo', label: 'Roll no' },
          { key: 'studentName', label: 'Student' },
          { key: 'description', label: 'Description' },
          { key: 'amount', label: 'Amount', render: (i) => money(i.amount) },
          { key: 'amountPaid', label: 'Paid', render: (i) => money(i.amountPaid) },
          { key: 'balance', label: 'Balance', render: (i) => money(i.balance) },
          { key: 'dueDate', label: 'Due' },
          { key: 'status', label: 'Status', render: (i) => <Badge tone={TONE[i.status]}>{i.status}</Badge> },
        ]} actions={(inv) => (
          <>
            {inv.status !== 'paid' && <button className="btn btn-small btn-primary" onClick={() => setModal({ mode: 'pay', inv })}>Record payment</button>}
            <button className="btn btn-small" onClick={() => setModal({ mode: 'history', inv })}>History</button>
            {inv.amountPaid === 0 && <button className="btn btn-small btn-danger" onClick={() => remove(inv)}>Delete</button>}
          </>
        )} />
      )}

      {modal?.mode === 'invoice' && (
        <Modal title="New invoice" onClose={() => setModal(null)}>
          <EntityForm onCancel={() => setModal(null)} submitLabel="Create invoice(s)"
            fields={[
              { name: 'studentId', label: 'Student', type: 'select', numeric: true, options: toOptions(lk.students, 'students'),
                hint: 'Choose a student OR a semester below.' },
              { name: 'semester', label: '…or bill every active student in semester', type: 'number', min: 1, max: 20 },
              { name: 'description', label: 'Description', required: true, placeholder: 'Tuition fee — Sem 1', full: true },
              { name: 'amount', label: 'Amount', type: 'number', min: 0.01, required: true },
              { name: 'dueDate', label: 'Due date', type: 'date' },
            ]}
            onSubmit={async (v) => {
              const created = await api.fees.create(v);
              setModal(null);
              setNotice(`${created.length} invoice(s) created.`);
              reload();
            }} />
        </Modal>
      )}
      {modal?.mode === 'pay' && (
        <Modal title={`Payment — ${modal.inv.studentName}`} onClose={() => setModal(null)}>
          <p className="muted">Outstanding balance: <strong>{money(modal.inv.balance)}</strong></p>
          <EntityForm onCancel={() => setModal(null)} submitLabel="Record payment"
            initial={{ amount: modal.inv.balance, method: 'cash' }}
            fields={[
              { name: 'amount', label: 'Amount', type: 'number', min: 0.01, max: modal.inv.balance, required: true },
              { name: 'method', label: 'Method', type: 'select', required: true,
                options: ['cash', 'card', 'bank_transfer', 'upi', 'cheque', 'online', 'other'].map((v) => ({ value: v, label: v.replace('_', ' ') })) },
              { name: 'reference', label: 'Reference / receipt no', full: true },
            ]}
            onSubmit={async (v) => {
              await api.fees.pay(modal.inv.id, v);
              setModal(null);
              setNotice('Payment recorded.');
              reload();
            }} />
        </Modal>
      )}
      {modal?.mode === 'history' && <PaymentHistory inv={modal.inv} onClose={() => setModal(null)} />}
    </>
  );
}

function PaymentHistory({ inv, onClose }) {
  const { api } = useCollege();
  const { data, error } = useAsync(() => api.fees.payments(inv.id), [api, inv.id]);
  return (
    <Modal title={`Payments — ${inv.description}`} onClose={onClose}>
      <ErrorBanner error={error} />
      {!data ? !error && <Spinner /> : (
        <DataTable rows={data} empty="No payments yet." columns={[
          { key: 'paidOn', label: 'Date', render: (p) => new Date(p.paidOn).toLocaleString() },
          { key: 'amount', label: 'Amount', render: (p) => money(p.amount) },
          { key: 'method', label: 'Method' },
          { key: 'reference', label: 'Reference' },
        ]} />
      )}
    </Modal>
  );
}
