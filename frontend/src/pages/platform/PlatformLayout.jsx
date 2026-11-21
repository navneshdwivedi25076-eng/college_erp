import { Navigate, NavLink, Outlet } from 'react-router';
import { useAuth } from '../../auth/AuthContext';

export default function PlatformLayout() {
  const { session, signOut } = useAuth();
  if (session?.kind !== 'platform') return <Navigate to="/platform/login" replace />;

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="sidebar-brand">🎓 College ERP<small>Platform admin</small></div>
        <nav>
          <NavLink to="colleges">Colleges</NavLink>
          <NavLink to="admins">Platform admins</NavLink>
        </nav>
        <div className="sidebar-foot">
          <div className="small">{session.profile?.email}</div>
          <button className="btn btn-ghost btn-small" onClick={signOut}>Sign out</button>
        </div>
      </aside>
      <main className="content"><Outlet /></main>
    </div>
  );
}
