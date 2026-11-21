/** Sidebar entries per role (single place to change who sees what). */
const ADMIN = 'college_admin';
const FACULTY = 'faculty';
const ACCOUNTANT = 'accountant';
const STUDENT = 'student';

export const collegeNav = [
  { to: 'dashboard', label: 'Dashboard', roles: [ADMIN, FACULTY, ACCOUNTANT] },
  { to: 'students', label: 'Students', roles: [ADMIN, FACULTY, ACCOUNTANT] },
  { to: 'faculty', label: 'Faculty', roles: [ADMIN, FACULTY, ACCOUNTANT] },
  { to: 'departments', label: 'Departments', roles: [ADMIN, FACULTY, ACCOUNTANT] },
  { to: 'courses', label: 'Courses', roles: [ADMIN, FACULTY, ACCOUNTANT] },
  { to: 'enrollments', label: 'Enrollments', roles: [ADMIN, FACULTY] },
  { to: 'attendance', label: 'Attendance', roles: [ADMIN, FACULTY] },
  { to: 'exams', label: 'Exams & marks', roles: [ADMIN, FACULTY] },
  { to: 'fees', label: 'Fees', roles: [ADMIN, ACCOUNTANT] },
  { to: 'users', label: 'Users', roles: [ADMIN] },
  { to: 'settings', label: 'Settings', roles: [ADMIN] },
  { to: 'my/courses', label: 'My courses', roles: [STUDENT] },
  { to: 'my/marks', label: 'My marks', roles: [STUDENT] },
  { to: 'my/fees', label: 'My fees', roles: [STUDENT] },
  { to: 'my/profile', label: 'My profile', roles: [STUDENT] },
];

export const canWrite = {
  departments: [ADMIN], faculty: [ADMIN], students: [ADMIN], courses: [ADMIN],
  exams: [ADMIN, FACULTY], users: [ADMIN],
};

export const homeFor = (role) => (role === STUDENT ? 'my/courses' : 'dashboard');
