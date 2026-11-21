import { useMemo } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { createCollegeApi } from '../../api/collegeApi';
import { useAuth } from '../../auth/AuthContext';
import LoginCard from '../../components/LoginCard';
import { homeFor } from '../../config/navigation';

export default function CollegeLogin() {
  const { code } = useParams();
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const api = useMemo(() => createCollegeApi(code), [code]);

  async function login(email, password) {
    const res = await api.login(email, password);
    signIn({
      kind: 'college', token: res.accessToken, collegeCode: res.profile.collegeCode,
      collegeName: res.profile.collegeName, user: res.profile.user,
    });
    navigate(`/c/${code}/${homeFor(res.profile.user.role)}`);
  }

  return (
    <LoginCard title="Sign in" subtitle={<>College: <code>{code}</code></>} onLogin={login}
      footer={<p className="muted small"><Link to="/">← Choose a different college</Link></p>} />
  );
}
