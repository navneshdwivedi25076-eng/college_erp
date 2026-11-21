import { useMemo } from 'react';
import { Navigate, NavLink, Outlet, useLocation, useParams } from 'react-router';
import { createCollegeApi } from '../../api/collegeApi';
import { useAuth } from '../../auth/AuthContext';
import { collegeNav, homeFor } from '../../config/navigation';
import { CollegeContext } from './CollegeContext';

export default function CollegeLayout() {
  const { code } = useParams();
  const { session, signOut } = useAuth();
  const location = useLocation();
  const api = useMemo(() => createCollegeApi(code), [code]);

  if (session?.kind !== 'college' || session.collegeCode !== code) return <Navigate to={`/c/${code}/login`} replace />;
  const { user } = session;
  if (location.pathname.replace(/\/$/, '') === `/c/${code}`) return <Navigate to={homeFor(user.role)} replace />;

  const ctx = { api, user, collegeName: session.collegeName, code };
  return (
    <CollegeContext value={ctx}>
      <div className="shell">
        <aside className="sidebar">
          <div className="sidebar-brand">🎓 {session.collegeName}<small>{code}</small></div>
          <nav>
            {collegeNav.filter((n) => n.roles.includes(user.role)).map((n) => (
              <NavLink key={n.to} to={n.to}>{n.label}</NavLink>
            ))}
          </nav>
          <div className="sidebar-foot">
            <div className="small">{user.fullName}</div>
            <div className="small muted">{user.role.replace('_', ' ')}</div>
            <button className="btn btn-ghost btn-small" onClick={signOut}>Sign out</button>
          </div>
        </aside>
        <main className="content"><Outlet /></main>
      </div>
    </CollegeContext>
  );
}
