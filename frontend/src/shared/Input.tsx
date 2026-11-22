import './Input.css'

type InputProps = {
  label: string
  name: string
  value: string
  onChange: (value: string) => void
  type?: 'text' | 'email' | 'password' | 'date'
  error?: string
  disabled?: boolean
  // Tells the browser what the field is, so it can fill it in or save it,
  // for example "email", "current-password" or "new-password".
  autoComplete?: string
  // The id of a <datalist>: the browser shows its options as suggestions
  // while the user types, and the user can still type any other text.
  list?: string
  // The most characters the browser lets the user type.
  maxLength?: number
}

function Input({
  label,
  name,
  value,
  onChange,
  type = 'text',
  error,
  disabled = false,
  autoComplete,
  list,
  maxLength,
}: InputProps) {
  return (
    <div className="field">
      <label className="field-label" htmlFor={name}>
        {label}
      </label>
      <input
        className={error ? 'field-input field-input-error' : 'field-input'}
        id={name}
        name={name}
        type={type}
        value={value}
        disabled={disabled}
        autoComplete={autoComplete}
        list={list}
        maxLength={maxLength}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${name}-error` : undefined}
        onChange={(event) => onChange(event.target.value)}
      />
      {error && (
        <small className="field-error" id={`${name}-error`}>
          {error}
        </small>
      )}
    </div>
  )
}

export default Input
