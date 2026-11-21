import { useState } from 'react';
import DataTable from '../../components/DataTable';
import ResourcePage from '../../components/ResourcePage';
import { ErrorBanner, Modal, Spinner } from '../../components/ui';
import { canWrite } from '../../config/navigation';
import { resourceConfigs } from '../../config/resources';
import { useAsync } from '../../hooks/useAsync';
import { nameById, useLookups } from '../../hooks/useLookups';
import { useCollege } from './CollegeContext';

export default function ExamsPage() {
  const { api, user } = useCollege();
  const lk = useLookups(api, ['courses']);
  const [marking, setMarking] = useState(null);
  const config = resourceConfigs.exams({
    // Faculty may only create exams for their own courses.
    courses: user.role === 'faculty' ? lk.courses.filter((c) => c.facultyId === user.facultyId) : lk.courses,
  });
  config.columns[1].render = (e) => nameById(lk.courses, 'courses', e.courseId);

  return (
    <>
      <ResourcePage {...config} title="Exams" resource={api.resource('exams')}
        canWrite={canWrite.exams.includes(user.role)}
        extraActions={(exam) => <button className="btn btn-small" onClick={() => setMarking(exam)}>Marks</button>} />
      {marking && <MarksEditor exam={marking} onClose={() => setMarking(null)} />}
    </>
  );
}

function MarksEditor({ exam, onClose }) {
  const { api } = useCollege();
  const [values, setValues] = useState({});
  const [saveError, setSaveError] = useState(null);
  const [saved, setSaved] = useState(false);

  const { data, error } = useAsync(async () => {
    const [roster, marks] = await Promise.all([
      api.enrollments.list({ courseId: exam.courseId }),
      api.marks.list({ examId: exam.id }),
    ]);
    const students = [...new Map(roster.map((r) => [r.studentId, r])).values()];
    setValues(Object.fromEntries(marks.map((m) => [m.studentId, m.marksObtained])));
    return students;
  }, [api, exam.id]);

  async function save() {
    setSaveError(null);
    try {
      const entries = Object.entries(values)
        .filter(([, v]) => v !== '' && v !== null && v !== undefined)
        .map(([studentId, v]) => ({ studentId: Number(studentId), marksObtained: Number(v) }));
      await api.marks.record(exam.id, entries);
      setSaved(true);
    } catch (e) {
      setSaveError(e);
    }
  }

  return (
    <Modal title={`${exam.name} — marks (out of ${exam.maxMarks})`} onClose={onClose}>
      <ErrorBanner error={error || saveError} />
      {saved && <div className="alert alert-ok">Marks saved.</div>}
      {!data ? !error && <Spinner /> : (
        <>
          <DataTable rows={data} empty="No students are enrolled in this course."
            columns={[
              { key: 'rollNo', label: 'Roll no' },
              { key: 'studentName', label: 'Student' },
              { key: 'marks', label: 'Marks', render: (r) => (
                <input type="number" min="0" max={exam.maxMarks} step="any" className="narrow"
                  value={values[r.studentId] ?? ''} aria-label={`Marks for ${r.studentName}`}
                  onChange={(e) => { setSaved(false); setValues({ ...values, [r.studentId]: e.target.value }); }} />
              ) },
            ]} />
          <div className="form-actions">
            <button className="btn btn-ghost" onClick={onClose}>Close</button>
            <button className="btn btn-primary" onClick={save} disabled={!data.length}>Save marks</button>
          </div>
        </>
      )}
    </Modal>
  );
}
