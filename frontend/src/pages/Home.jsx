import { useNavigate } from 'react-router';

export default function Home() {
  const navigate = useNavigate();

  function openCollege(formData) {
    const code = String(formData.get('code') ?? '').trim().toLowerCase();
    if (code) navigate(`/c/${code}/login`);
  }

  return (
    <div className="auth-shell">
      <div className="brand-hero">
        <div className="logo">🎓</div>
        <h1>College ERP</h1>
        <p className="muted">One platform, any number of colleges — each with its own private database.</p>
      </div>
      <div className="home-grid">
        <div className="card">
          <h2>College portal</h2>
          <p className="muted">Admins, faculty, accountants and students sign in here.</p>
          <form action={openCollege} className="form">
            <div className="field">
              <label htmlFor="code">College code</label>
              <input id="code" name="code" placeholder="e.g. stxavier" required />
            </div>
            <button className="btn btn-primary">Continue</button>
          </form>
        </div>
        <div className="card">
          <h2>Platform administration</h2>
          <p className="muted">Onboard new colleges, manage plans, suspend or remove tenants.</p>
          <button className="btn" onClick={() => navigate('/platform/login')}>Platform admin sign-in</button>
        </div>
      </div>
    </div>
  );
}
