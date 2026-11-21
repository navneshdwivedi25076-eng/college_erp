import { Link, useNavigate } from 'react-router';
import { platformApi } from '../../api/platformApi';
import { useAuth } from '../../auth/AuthContext';
import LoginCard from '../../components/LoginCard';

export default function PlatformLogin() {
  const { signIn } = useAuth();
  const navigate = useNavigate();

  async function login(email, password) {
    const res = await platformApi.login(email, password);
    signIn({ kind: 'platform', token: res.accessToken, profile: res.profile });
    navigate('/platform/colleges');
  }

  return (
    <LoginCard title="Platform admin" subtitle="Manage every college on the platform." onLogin={login}
      footer={<p className="muted small"><Link to="/">← Back</Link></p>} />
  );
}
