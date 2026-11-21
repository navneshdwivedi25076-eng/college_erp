import { useActionState } from 'react';
import { ErrorBanner } from './ui';

/**
 * Schema-driven form built on React 19 form Actions.
 * fields: [{ name, label, type, required, options, min, max, step, placeholder, full }]
 * types: text | email | password | number | date | textarea | select | checkbox
 */
export default function EntityForm({ fields, initial = {}, onSubmit, submitLabel = 'Save', onCancel }) {
  const [error, submitAction, pending] = useActionState(async (_prev, formData) => {
    try {
      await onSubmit(readValues(fields, formData));
      return null;
    } catch (e) {
      return e;
    }
  }, null);

  return (
    <form action={submitAction} className="form">
      <ErrorBanner error={error} />
      <div className="form-grid">
        {fields.map((f) => (
          <Field key={f.name} field={f} value={initial[f.name]} />
        ))}
      </div>
      <div className="form-actions">
        {onCancel && <button type="button" className="btn btn-ghost" onClick={onCancel}>Cancel</button>}
        <button className="btn btn-primary" disabled={pending}>{pending ? 'Saving…' : submitLabel}</button>
      </div>
    </form>
  );
}

function Field({ field: f, value }) {
  const common = { id: `f-${f.name}`, name: f.name, required: f.required, placeholder: f.placeholder };
  let input;
  if (f.type === 'textarea') {
    input = <textarea {...common} defaultValue={value ?? ''} rows={3} />;
  } else if (f.type === 'select') {
    input = (
      <select {...common} defaultValue={value ?? ''}>
        <option value="">{f.required ? 'Select…' : '— none —'}</option>
        {(f.options ?? []).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
      </select>
    );
  } else if (f.type === 'checkbox') {
    return (
      <label className={`checkbox ${f.full ? 'full' : ''}`}>
        <input type="checkbox" name={f.name} defaultChecked={value ?? f.defaultChecked ?? false} /> {f.label}
      </label>
    );
  } else {
    input = (
      <input {...common} type={f.type ?? 'text'} defaultValue={value ?? f.defaultValue ?? ''}
        min={f.min} max={f.max} step={f.step ?? (f.type === 'number' ? 'any' : undefined)} />
    );
  }
  return (
    <div className={`field ${f.full || f.type === 'textarea' ? 'full' : ''}`}>
      <label htmlFor={common.id}>{f.label}{f.required && <span className="req"> *</span>}</label>
      {input}
      {f.hint && <small className="muted">{f.hint}</small>}
    </div>
  );
}

/** Converts FormData to typed JSON: numbers, nulls for blanks, booleans for checkboxes. */
function readValues(fields, fd) {
  const out = {};
  for (const f of fields) {
    if (f.type === 'checkbox') {
      out[f.name] = fd.get(f.name) === 'on';
      continue;
    }
    const raw = fd.get(f.name);
    if (raw === null || raw === '') {
      out[f.name] = f.omitIfBlank ? undefined : null;
    } else if (f.type === 'number' || f.numeric) {
      out[f.name] = Number(raw);
    } else {
      out[f.name] = raw;
    }
  }
  return out;
}
