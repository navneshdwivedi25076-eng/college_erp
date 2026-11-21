import { createContext, use } from 'react';

/** Provides the college-scoped API client and current user to college pages. */
export const CollegeContext = createContext(null);

export function useCollege() {
  const ctx = use(CollegeContext);
  if (!ctx) throw new Error('useCollege must be used inside a college layout');
  return ctx;
}
