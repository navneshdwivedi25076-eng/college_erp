import { createContext, use, useCallback, useMemo, useState } from 'react';
import { configureHttp } from '../api/http';

const STORAGE_KEY = 'college-erp.session';
const AuthContext = createContext(null);

function loadSession() {
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY)) ?? null;
  } catch {
    return null;
  }
}

/**
 * Holds the current session. A session is either
 *   { kind: 'platform', token, profile }                       -> super admin
 *   { kind: 'college', token, collegeCode, collegeName, user } -> a college user
 */
export function AuthProvider({ children }) {
  const [session, setSession] = useState(loadSession);

  const signIn = useCallback((next) => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    setSession(next);
  }, []);

  const signOut = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY);
    setSession(null);
  }, []);

  configureHttp({ getToken: () => session?.token ?? null, handleUnauthorized: signOut });

  const value = useMemo(() => ({ session, signIn, signOut }), [session, signIn, signOut]);
  return <AuthContext value={value}>{children}</AuthContext>;
}

export function useAuth() {
  const ctx = use(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}
