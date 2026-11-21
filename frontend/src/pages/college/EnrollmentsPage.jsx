import { useState } from 'react';
import DataTable from '../../components/DataTable';
import { ErrorBanner, Notice, PageHeader } from '../../components/ui';
import { useAsync } from '../../hooks/useAsync';
import { toOptions, useLookups } from '../../hooks/useLookups';
import { useCollege } from './CollegeContext';

const currentYear = () => {
  const d = new Date();
  const y = d.getMonth() >= 5 ? d.getFullYear() : d.getFullYear() - 1;
  return `${y}-${String((y + 1) % 100).padStart(2, '0')}`;
};

export default function EnrollmentsPage() {
  const { api, user } = useCollege();
  const lk = useLookups(api, ['courses', 'students']);
  const [courseId, setCourseId] = useState('');
  const [year, setYear] = useState(currentYear());
  const [selected, setSelected] = useState([]);
  const [notice, setNotice] = useState('');
  const [actionError, setActionError] = useState(null);
  const isAdmin = user.role === 'college_admin';

  const { data: enrolled, error, reload } = useAsync(
    () => (courseId ? api.enrollments.list({ courseId, academicYear: year }) : Promise.resolve([])),
    [api, courseId, year],
  );
  const enrolledIds = new Set((enrolled ?? []).map((e) => e.studentId));
  const candidates = lk.students.filter((s) => !enrolledIds.has(s.id) && s.status === 'active');

  async function enroll() {
    setActionError(null);
    try {
      const r = await api.enrollments.enroll({ courseId: Number(courseId), studentIds: selected, academicYear: year });
      setNotice(`${r.added} student(s) enrolled.`);
      setSelected([]);
      reload();
    } catch (e) {
      setActionError(e);
    }
  }

  async function unenroll(row) {
    if (!confirm(`Remove ${row.studentName} from this course?`)) return;
    await api.enrollments.remove(row.id).catch(setActionError);
    reload();
  }

  return (
    <>
      <PageHeader title="Enrollments" subtitle="Assign students to courses for an academic year." />
      <div className="toolbar">
        <select value={courseId} onChange={(e) => setCourseId(e.target.value)} aria-label="Course">
          <option value="">Select course…</option>
          {toOptions(lk.courses, 'courses').map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
        <input value={year} onChange={(e) => setYear(e.target.value)} aria-label="Academic year" style={{ width: 110 }} />
      </div>
      <Notice>{notice}</Notice>
      <ErrorBanner error={error || actionError} />
      {courseId && (
        <div className="split">
          <section>
            <h2 className="section-title">Enrolled ({enrolled?.length ?? 0})</h2>
            <DataTable rows={enrolled} columns={[
              { key: 'rollNo', label: 'Roll no' },
              { key: 'studentName', label: 'Student' },
            ]} actions={isAdmin ? (r) => <button className="btn btn-small btn-danger" onClick={() => unenroll(r)}>Remove</button> : undefined} />
          </section>
          {isAdmin && (
            <section>
              <h2 className="section-title">Add students</h2>
              <div className="checklist">
                {candidates.length === 0 && <div className="empty">Everyone active is already enrolled.</div>}
                {candidates.map((s) => (
                  <label key={s.id} className="checkbox">
                    <input type="checkbox" checked={selected.includes(s.id)}
                      onChange={(e) => setSelected(e.target.checked ? [...selected, s.id] : selected.filter((x) => x !== s.id))} />
                    {s.rollNo} – {s.firstName} {s.lastName}
                  </label>
                ))}
              </div>
              <div className="row">
                <button className="btn btn-small" onClick={() => setSelected(candidates.map((s) => s.id))}>Select all</button>
                <button className="btn btn-primary" disabled={!selected.length} onClick={enroll}>Enroll {selected.length || ''}</button>
              </div>
            </section>
          )}
        </div>
      )}
    </>
  );
}
