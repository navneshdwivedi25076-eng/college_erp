import { Navigate, Route, Routes } from 'react-router';
import Home from './pages/Home';
import PlatformLogin from './pages/platform/PlatformLogin';
import PlatformLayout from './pages/platform/PlatformLayout';
import CollegesPage from './pages/platform/CollegesPage';
import AdminsPage from './pages/platform/AdminsPage';
import CollegeLogin from './pages/college/CollegeLogin';
import CollegeLayout from './pages/college/CollegeLayout';
import Dashboard from './pages/college/Dashboard';
import EntityScreen from './pages/college/EntityScreen';
import EnrollmentsPage from './pages/college/EnrollmentsPage';
import AttendancePage from './pages/college/AttendancePage';
import ExamsPage from './pages/college/ExamsPage';
import FeesPage from './pages/college/FeesPage';
import SettingsPage from './pages/college/SettingsPage';
import { MyCourses, MyFees, MyMarks, MyProfile } from './pages/college/StudentPortal';

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />

      <Route path="/platform/login" element={<PlatformLogin />} />
      <Route path="/platform" element={<PlatformLayout />}>
        <Route index element={<Navigate to="colleges" replace />} />
        <Route path="colleges" element={<CollegesPage />} />
        <Route path="admins" element={<AdminsPage />} />
      </Route>

      <Route path="/c/:code/login" element={<CollegeLogin />} />
      <Route path="/c/:code" element={<CollegeLayout />}>
        <Route path="dashboard" element={<Dashboard />} />
        <Route path="students" element={<EntityScreen name="students" />} />
        <Route path="faculty" element={<EntityScreen name="faculty" />} />
        <Route path="departments" element={<EntityScreen name="departments" />} />
        <Route path="courses" element={<EntityScreen name="courses" />} />
        <Route path="users" element={<EntityScreen name="users" />} />
        <Route path="enrollments" element={<EnrollmentsPage />} />
        <Route path="attendance" element={<AttendancePage />} />
        <Route path="exams" element={<ExamsPage />} />
        <Route path="fees" element={<FeesPage />} />
        <Route path="settings" element={<SettingsPage />} />
        <Route path="my/courses" element={<MyCourses />} />
        <Route path="my/marks" element={<MyMarks />} />
        <Route path="my/fees" element={<MyFees />} />
        <Route path="my/profile" element={<MyProfile />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
