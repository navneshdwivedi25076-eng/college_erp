// Single responsibility: HTTP transport, auth header, and error normalisation.
const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '';

export class ApiError extends Error {
  constructor(status, problem) {
    const fieldErrors = problem?.errors ? Object.values(problem.errors).flat().join(' ') : '';
    super(problem?.detail || fieldErrors || problem?.title || `Request failed (${status})`);
    this.status = status;
    this.problem = problem;
  }
}

let tokenProvider = () => null;
let onUnauthorized = () => {};

/** Lets the auth layer plug in token lookup and logout (dependency inversion). */
export function configureHttp({ getToken, handleUnauthorized }) {
  tokenProvider = getToken;
  onUnauthorized = handleUnauthorized;
}

export async function request(method, path, body) {
  const headers = { Accept: 'application/json' };
  const token = tokenProvider();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  const res = await fetch(`${BASE_URL}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (res.status === 204) return null;
  const text = await res.text();
  const data = text ? JSON.parse(text) : null;
  if (!res.ok) {
    if (res.status === 401 && token) onUnauthorized();
    throw new ApiError(res.status, data);
  }
  return data;
}

export const http = {
  get: (p) => request('GET', p),
  post: (p, b) => request('POST', p, b ?? {}),
  put: (p, b) => request('PUT', p, b),
  del: (p) => request('DELETE', p),
};

export function toQuery(params = {}) {
  const q = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') q.set(k, v);
  });
  const s = q.toString();
  return s ? `?${s}` : '';
}
