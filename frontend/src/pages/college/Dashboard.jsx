import { ErrorBanner, PageHeader, Spinner, StatCard, money } from '../../components/ui';
import { useAsync } from '../../hooks/useAsync';
import { useCollege } from './CollegeContext';

export default function Dashboard() {
  const { api, collegeName } = useCollege();
  const { data, error } = useAsync(() => api.dashboard(), [api]);

  return (
    <>
      <PageHeader title="Dashboard" subtitle={collegeName} />
      <ErrorBanner error={error} />
      {!data ? !error && <Spinner /> : (
        <>
          <div className="stats-grid">
            <StatCard label="Students" value={`${data.students}${data.maxStudents ? ` / ${data.maxStudents}` : ''}`} />
            <StatCard label="Active students" value={data.activeStudents} />
            <StatCard label="Faculty" value={data.faculty} />
            <StatCard label="Departments" value={data.departments} />
            <StatCard label="Courses" value={data.courses} />
            <StatCard label="User accounts" value={data.users} />
          </div>
          <h2 className="section-title">Fees</h2>
          <div className="stats-grid">
            <StatCard label="Billed" value={money(data.feesBilled)} />
            <StatCard label="Collected" value={money(data.feesCollected)} />
            <StatCard label="Outstanding" value={money(data.feesOutstanding)} />
          </div>
          <p className="muted small">Plan: {data.plan}</p>
        </>
      )}
    </>
  );
}
