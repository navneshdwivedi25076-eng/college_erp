import { useActionState } from 'react';
import { ErrorBanner } from './ui';

/** Shared email/password login form (React 19 form action). */
export default function LoginCard({ title, subtitle, onLogin, footer }) {
  const [error, action, pending] = useActionState(async (_prev, fd) => {
    try {
      await onLogin(String(fd.get('email')), String(fd.get('password')));
      return null;
    } catch (e) {
      return e;
    }
  }, null);

  return (
    <div className="auth-shell">
      <div className="card auth-card">
        <h1>{title}</h1>
        {subtitle && <p className="muted">{subtitle}</p>}
        <ErrorBanner error={error} />
        <form action={action} className="form">
          <div className="field">
            <label htmlFor="email">Email</label>
            <input id="email" name="email" type="email" autoComplete="username" required />
          </div>
          <div className="field">
            <label htmlFor="password">Password</label>
            <input id="password" name="password" type="password" autoComplete="current-password" required />
          </div>
          <button className="btn btn-primary" disabled={pending}>{pending ? 'Signing in…' : 'Sign in'}</button>
        </form>
        {footer}
      </div>
    </div>
  );
}
