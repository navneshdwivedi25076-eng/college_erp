import { http, toQuery } from './http';

/** Super-admin API: onboarding and managing colleges. */
export const platformApi = {
  login: (email, password) => http.post('/api/platform/auth/login', { email, password }),
  listColleges: (params) => http.get(`/api/platform/colleges${toQuery(params)}`),
  createCollege: (data) => http.post('/api/platform/colleges', data),
  updateCollege: (id, data) => http.put(`/api/platform/colleges/${id}`, data),
  suspend: (id) => http.post(`/api/platform/colleges/${id}/suspend`),
  activate: (id) => http.post(`/api/platform/colleges/${id}/activate`),
  deleteCollege: (id, dropDatabase) => http.del(`/api/platform/colleges/${id}${toQuery({ dropDatabase })}`),
  stats: (id) => http.get(`/api/platform/colleges/${id}/stats`),
  migrateAll: () => http.post('/api/platform/colleges/migrate'),
  listAdmins: () => http.get('/api/platform/admins'),
  createAdmin: (data) => http.post('/api/platform/admins', data),
};
