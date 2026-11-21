import { http, toQuery } from './http';

/** Factory: every call is scoped to one college's API (and therefore its own database). */
export function createCollegeApi(code) {
  const base = `/api/t/${encodeURIComponent(code)}`;

  /** Generic REST resource client — reused by every CRUD screen (open for extension). */
  const resource = (path) => ({
    list: (params) => http.get(`${base}/${path}${toQuery(params)}`),
    get: (id) => http.get(`${base}/${path}/${id}`),
    create: (data) => http.post(`${base}/${path}`, data),
    update: (id, data) => http.put(`${base}/${path}/${id}`, data),
    remove: (id) => http.del(`${base}/${path}/${id}`),
  });

  return {
    code,
    login: (email, password) => http.post(`${base}/auth/login`, { email, password }),
    me: () => http.get(`${base}/auth/me`),
    dashboard: () => http.get(`${base}/dashboard`),
    resource,
    users: resource('users'),
    enrollments: {
      list: (params) => http.get(`${base}/enrollments${toQuery(params)}`),
      enroll: (data) => http.post(`${base}/enrollments`, data),
      remove: (id) => http.del(`${base}/enrollments/${id}`),
    },
    attendance: {
      list: (params) => http.get(`${base}/attendance${toQuery(params)}`),
      mark: (data) => http.post(`${base}/attendance`, data),
      summary: (courseId) => http.get(`${base}/attendance/summary${toQuery({ courseId })}`),
    },
    marks: {
      list: (params) => http.get(`${base}/marks${toQuery(params)}`),
      record: (examId, entries) => http.post(`${base}/exams/${examId}/marks`, { entries }),
    },
    fees: {
      list: (params) => http.get(`${base}/fees/invoices${toQuery(params)}`),
      create: (data) => http.post(`${base}/fees/invoices`, data),
      remove: (id) => http.del(`${base}/fees/invoices/${id}`),
      payments: (id) => http.get(`${base}/fees/invoices/${id}/payments`),
      pay: (id, data) => http.post(`${base}/fees/invoices/${id}/payments`, data),
    },
    settings: {
      get: () => http.get(`${base}/settings`),
      save: (values) => http.put(`${base}/settings`, values),
    },
    portal: {
      profile: () => http.get(`${base}/me/profile`),
      courses: () => http.get(`${base}/me/courses`),
      marks: () => http.get(`${base}/me/marks`),
      invoices: () => http.get(`${base}/me/invoices`),
    },
  };
}
