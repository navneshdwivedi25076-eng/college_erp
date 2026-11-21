import { nameById, toOptions } from '../hooks/useLookups';

/**
 * Declarative screen definitions. Adding a new entity screen = adding an entry here
 * (Open/Closed) — ResourcePage renders it.
 */
const STUDENT_STATUS = ['active', 'graduated', 'dropped', 'suspended'].map((v) => ({ value: v, label: v }));
const ROLES = [
  { value: 'college_admin', label: 'College admin' },
  { value: 'faculty', label: 'Faculty' },
  { value: 'accountant', label: 'Accountant' },
  { value: 'student', label: 'Student' },
];

export const resourceConfigs = {
  departments: () => ({
    title: 'Departments',
    columns: [
      { key: 'code', label: 'Code' },
      { key: 'name', label: 'Name' },
      { key: 'description', label: 'Description' },
    ],
    fields: [
      { name: 'code', label: 'Code', required: true },
      { name: 'name', label: 'Name', required: true },
      { name: 'description', label: 'Description', type: 'textarea' },
    ],
  }),

  faculty: (lk) => ({
    title: 'Faculty',
    columns: [
      { key: 'employeeNo', label: 'Emp. no' },
      { key: 'name', label: 'Name', render: (f) => `${f.firstName} ${f.lastName}` },
      { key: 'designation', label: 'Designation' },
      { key: 'departmentId', label: 'Department', render: (f) => nameById(lk.departments, 'departments', f.departmentId) },
      { key: 'email', label: 'Email' },
    ],
    fields: [
      { name: 'employeeNo', label: 'Employee no', required: true },
      { name: 'designation', label: 'Designation' },
      { name: 'firstName', label: 'First name', required: true },
      { name: 'lastName', label: 'Last name', required: true },
      { name: 'email', label: 'Email', type: 'email' },
      { name: 'phone', label: 'Phone' },
      { name: 'departmentId', label: 'Department', type: 'select', numeric: true, options: toOptions(lk.departments, 'departments') },
      { name: 'joinedOn', label: 'Joined on', type: 'date' },
    ],
  }),

  students: (lk) => ({
    title: 'Students',
    columns: [
      { key: 'rollNo', label: 'Roll no' },
      { key: 'name', label: 'Name', render: (s) => `${s.firstName} ${s.lastName}` },
      { key: 'departmentId', label: 'Department', render: (s) => nameById(lk.departments, 'departments', s.departmentId) },
      { key: 'currentSemester', label: 'Sem' },
      { key: 'status', label: 'Status' },
      { key: 'phone', label: 'Phone' },
    ],
    fields: [
      { name: 'rollNo', label: 'Roll no', required: true },
      { name: 'admissionYear', label: 'Admission year', type: 'number', min: 1900, max: 3000 },
      { name: 'firstName', label: 'First name', required: true },
      { name: 'lastName', label: 'Last name', required: true },
      { name: 'email', label: 'Email', type: 'email' },
      { name: 'phone', label: 'Phone' },
      { name: 'dateOfBirth', label: 'Date of birth', type: 'date' },
      { name: 'gender', label: 'Gender', type: 'select', options: ['female', 'male', 'other'].map((v) => ({ value: v, label: v })) },
      { name: 'departmentId', label: 'Department', type: 'select', numeric: true, options: toOptions(lk.departments, 'departments') },
      { name: 'currentSemester', label: 'Current semester', type: 'number', min: 1, max: 20, required: true, defaultValue: 1 },
      { name: 'status', label: 'Status', type: 'select', required: true, options: STUDENT_STATUS },
      { name: 'guardianName', label: 'Guardian name' },
      { name: 'guardianPhone', label: 'Guardian phone' },
      { name: 'address', label: 'Address', type: 'textarea' },
    ],
  }),

  courses: (lk) => ({
    title: 'Courses',
    columns: [
      { key: 'code', label: 'Code' },
      { key: 'title', label: 'Title' },
      { key: 'credits', label: 'Credits' },
      { key: 'semester', label: 'Sem' },
      { key: 'departmentId', label: 'Department', render: (c) => nameById(lk.departments, 'departments', c.departmentId) },
      { key: 'facultyId', label: 'Instructor', render: (c) => nameById(lk.faculty, 'faculty', c.facultyId) },
    ],
    fields: [
      { name: 'code', label: 'Code', required: true },
      { name: 'title', label: 'Title', required: true },
      { name: 'credits', label: 'Credits', type: 'number', min: 0, max: 50, defaultValue: 3 },
      { name: 'semester', label: 'Semester', type: 'number', min: 1, max: 20 },
      { name: 'departmentId', label: 'Department', type: 'select', numeric: true, options: toOptions(lk.departments, 'departments') },
      { name: 'facultyId', label: 'Instructor', type: 'select', numeric: true, options: toOptions(lk.faculty, 'faculty') },
    ],
  }),

  exams: (lk) => ({
    title: 'Exams',
    columns: [
      { key: 'name', label: 'Exam' },
      { key: 'courseId', label: 'Course', render: (e) => nameById(lk.courses, 'courses', e.courseId) },
      { key: 'maxMarks', label: 'Max marks' },
      { key: 'examDate', label: 'Date' },
    ],
    fields: [
      { name: 'courseId', label: 'Course', type: 'select', numeric: true, required: true, options: toOptions(lk.courses, 'courses') },
      { name: 'name', label: 'Name', required: true, placeholder: 'Midterm, Final, Quiz 1…' },
      { name: 'maxMarks', label: 'Max marks', type: 'number', min: 1, defaultValue: 100, required: true },
      { name: 'examDate', label: 'Exam date', type: 'date' },
    ],
  }),

  users: (lk) => {
    const links = [
      { name: 'studentId', label: 'Linked student (for student logins)', type: 'select', numeric: true, options: toOptions(lk.students, 'students') },
      { name: 'facultyId', label: 'Linked faculty (for faculty logins)', type: 'select', numeric: true, options: toOptions(lk.faculty, 'faculty') },
    ];
    return {
      title: 'Users',
      subtitle: 'Login accounts for this college.',
      columns: [
        { key: 'email', label: 'Email' },
        { key: 'fullName', label: 'Name' },
        { key: 'role', label: 'Role' },
        { key: 'isActive', label: 'Active', render: (u) => (u.isActive ? 'Yes' : 'No') },
      ],
      fields: [
        { name: 'email', label: 'Email', type: 'email', required: true },
        { name: 'fullName', label: 'Full name', required: true },
        { name: 'password', label: 'Password', type: 'password', required: true, hint: 'At least 8 characters.' },
        { name: 'role', label: 'Role', type: 'select', required: true, options: ROLES },
        ...links,
      ],
      editFields: [
        { name: 'fullName', label: 'Full name', required: true },
        { name: 'role', label: 'Role', type: 'select', required: true, options: ROLES },
        ...links,
        { name: 'password', label: 'New password (optional)', type: 'password', omitIfBlank: true },
        { name: 'isActive', label: 'Account active', type: 'checkbox', full: true },
      ],
    };
  },
};

/** Which lookups each screen needs. */
export const resourceLookups = {
  departments: [],
  faculty: ['departments'],
  students: ['departments'],
  courses: ['departments', 'faculty'],
  exams: ['courses'],
  users: ['students', 'faculty'],
};
