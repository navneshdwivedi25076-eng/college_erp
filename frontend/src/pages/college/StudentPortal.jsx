import DataTable from '../../components/DataTable';
import { Badge, ErrorBanner, PageHeader, Spinner, money } from '../../components/ui';
import { useAsync } from '../../hooks/useAsync';
import { useCollege } from './CollegeContext';

function PortalPage({ title, subtitle, load, children }) {
  const { api } = useCollege();
  const { data, error } = useAsync(() => load(api), [api]);
  return (
    <>
      <PageHeader title={title} subtitle={subtitle} />
      <ErrorBanner error={error} />
      {!data ? !error && <Spinner /> : children(data)}
    </>
  );
}

export function MyCourses() {
  return (
    <PortalPage title="My courses" subtitle="Courses you're enrolled in and your attendance." load={(api) => api.portal.courses()}>
      {(rows) => (
        <DataTable rows={rows} empty="You are not enrolled in any course yet." columns={[
          { key: 'code', label: 'Code' },
          { key: 'title', label: 'Course' },
          { key: 'credits', label: 'Credits' },
          { key: 'academicYear', label: 'Year' },
          { key: 'att', label: 'Attendance', render: (c) => (
            <span className={c.attendancePercentage < 75 && c.totalClasses ? 'text-warn' : ''}>
              {c.attended}/{c.totalClasses} ({c.attendancePercentage}%)
            </span>
          ) },
        ]} />
      )}
    </PortalPage>
  );
}

export function MyMarks() {
  return (
    <PortalPage title="My marks" load={(api) => api.portal.marks()}>
      {(rows) => (
        <DataTable rows={rows} empty="No marks published yet." columns={[
          { key: 'examName', label: 'Exam' },
          { key: 'marks', label: 'Marks', render: (m) => `${m.marksObtained} / ${m.maxMarks}` },
          { key: 'percentage', label: '%', render: (m) => `${m.percentage}%` },
          { key: 'remarks', label: 'Remarks' },
        ]} />
      )}
    </PortalPage>
  );
}

export function MyFees() {
  return (
    <PortalPage title="My fees" load={(api) => api.portal.invoices()}>
      {(rows) => (
        <DataTable rows={rows} empty="No invoices." columns={[
          { key: 'description', label: 'Description' },
          { key: 'amount', label: 'Amount', render: (i) => money(i.amount) },
          { key: 'amountPaid', label: 'Paid', render: (i) => money(i.amountPaid) },
          { key: 'balance', label: 'Balance', render: (i) => money(i.balance) },
          { key: 'dueDate', label: 'Due' },
          { key: 'status', label: 'Status', render: (i) => <Badge tone={{ paid: 'ok', partial: 'info', unpaid: 'warn' }[i.status]}>{i.status}</Badge> },
        ]} />
      )}
    </PortalPage>
  );
}

export function MyProfile() {
  return (
    <PortalPage title="My profile" load={(api) => api.portal.profile()}>
      {(s) => (
        <div className="card profile">
          {[
            ['Roll no', s.rollNo], ['Name', `${s.firstName} ${s.lastName}`], ['Email', s.email], ['Phone', s.phone],
            ['Date of birth', s.dateOfBirth], ['Semester', s.currentSemester], ['Admission year', s.admissionYear],
            ['Status', s.status], ['Guardian', s.guardianName], ['Guardian phone', s.guardianPhone], ['Address', s.address],
          ].map(([k, v]) => (
            <div key={k} className="profile-row"><span className="muted">{k}</span><span>{v ?? '—'}</span></div>
          ))}
        </div>
      )}
    </PortalPage>
  );
}
