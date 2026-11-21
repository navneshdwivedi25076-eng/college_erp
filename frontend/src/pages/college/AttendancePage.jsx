import { useEffect, useState } from 'react';
import DataTable from '../../components/DataTable';
import { ErrorBanner, Notice, PageHeader } from '../../components/ui';
import { useAsync } from '../../hooks/useAsync';
import { toOptions, useLookups } from '../../hooks/useLookups';
import { useCollege } from './CollegeContext';

const STATUSES = ['present', 'absent', 'late', 'excused'];
const today = () => new Date().toISOString().slice(0, 10);

export default function AttendancePage() {
  const { api, user } = useCollege();
  const lk = useLookups(api, ['courses']);
  const myCourses = user.role === 'faculty' ? lk.courses.filter((c) => c.facultyId === user.facultyId) : lk.courses;
  const [courseId, setCourseId] = useState('');
  const [date, setDate] = useState(today());
  const [marks, setMarks] = useState({});
  const [notice, setNotice] = useState('');
  const [saveError, setSaveError] = useState(null);

  const { data, error, reload } = useAsync(async () => {
    if (!courseId) return null;
    const [roster, existing, summary] = await Promise.all([
      api.enrollments.list({ courseId }),
      api.attendance.list({ courseId, date }),
      api.attendance.summary(courseId),
    ]);
    const unique = [...new Map(roster.map((r) => [r.studentId, r])).values()];
    return { roster: unique, existing, summary };
  }, [api, courseId, date]);

  useEffect(() => {
    if (!data) return;
    const m = {};
    data.roster.forEach((r) => { m[r.studentId] = 'present'; });
    data.existing.forEach((a) => { m[a.studentId] = a.status; });
    setMarks(m);
  }, [data]);

  async function save() {
    setSaveError(null);
    try {
      const entries = Object.entries(marks).map(([studentId, status]) => ({ studentId: Number(studentId), status }));
      await api.attendance.mark({ courseId: Number(courseId), date, entries });
      setNotice(`Attendance saved for ${entries.length} students on ${date}.`);
      reload();
    } catch (e) {
      setSaveError(e);
    }
  }

  return (
    <>
      <PageHeader title="Attendance" subtitle="Mark a class, then review each student's attendance percentage." />
      <div className="toolbar">
        <select value={courseId} onChange={(e) => { setCourseId(e.target.value); setNotice(''); }} aria-label="Course">
          <option value="">Select course…</option>
          {toOptions(myCourses, 'courses').map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)} aria-label="Date" />
      </div>
      <Notice>{notice}</Notice>
      <ErrorBanner error={error || saveError} />
      {data && (
        <div className="split">
          <section>
            <h2 className="section-title">Class of {date}</h2>
            <DataTable rows={data.roster} empty="No students enrolled in this course yet."
              columns={[
                { key: 'rollNo', label: 'Roll no' },
                { key: 'studentName', label: 'Student' },
                { key: 'status', label: 'Status', render: (r) => (
                  <div className="segmented">
                    {STATUSES.map((s) => (
                      <button key={s} type="button" className={marks[r.studentId] === s ? `on ${s}` : ''}
                        onClick={() => setMarks({ ...marks, [r.studentId]: s })}>{s}</button>
                    ))}
                  </div>
                ) },
              ]} />
            {data.roster.length > 0 && <button className="btn btn-primary" onClick={save}>Save attendance</button>}
          </section>
          <section>
            <h2 className="section-title">Summary</h2>
            <DataTable rows={data.summary} empty="No attendance recorded yet."
              columns={[
                { key: 'rollNo', label: 'Roll no' },
                { key: 'studentName', label: 'Student' },
                { key: 'attended', label: 'Attended', render: (s) => `${s.attended}/${s.totalClasses}` },
                { key: 'percentage', label: '%', render: (s) => <span className={s.percentage < 75 ? 'text-warn' : ''}>{s.percentage}%</span> },
              ]} />
          </section>
        </div>
      )}
    </>
  );
}
