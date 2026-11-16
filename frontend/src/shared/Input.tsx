import './Input.css'

type InputProps = {
  label: string
  name: string
  value: string
  onChange: (value: string) => void
  type?: 'text' | 'email' | 'password' | 'date'
  error?: string
  disabled?: boolean
}

function Input({
  label,
  name,
  value,
  onChange,
  type = 'text',
  error,
  disabled = false,
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
