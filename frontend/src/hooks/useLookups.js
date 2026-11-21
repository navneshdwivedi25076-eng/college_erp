import { useAsync } from './useAsync';

/**
 * Loads reference lists (departments, faculty, courses...) used by selects and
 * tables. Returns { departments: [...], faculty: [...], ... } plus label helpers.
 */
export function useLookups(api, names) {
  const key = names.join(',');
  const { data } = useAsync(async () => {
    const entries = await Promise.all(
      names.map(async (n) => [n, (await api.resource(n).list({ take: 500 })).items]),
    );
    return Object.fromEntries(entries);
  }, [api, key]);
  return data ?? Object.fromEntries(names.map((n) => [n, []]));
}

export const labelOf = {
  departments: (d) => `${d.code} – ${d.name}`,
  faculty: (f) => `${f.firstName} ${f.lastName} (${f.employeeNo})`,
  courses: (c) => `${c.code} – ${c.title}`,
  students: (s) => `${s.rollNo} – ${s.firstName} ${s.lastName}`,
  exams: (e) => e.name,
};

export function toOptions(list, name) {
  return (list ?? []).map((x) => ({ value: x.id, label: labelOf[name](x) }));
}

export function nameById(list, name, id) {
  const item = (list ?? []).find((x) => x.id === id);
  return item ? labelOf[name](item) : id ?? '—';
}
