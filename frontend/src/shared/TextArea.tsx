import './Input.css'

type TextAreaProps = {
  label: string
  name: string
  value: string
  onChange: (value: string) => void
  error?: string
  disabled?: boolean
  // How many lines of text the box shows before it scrolls.
  rows?: number
  // The most characters the browser lets the user type.
  maxLength?: number
}

// A text box with many lines, for a long text like a description.
// It has the same label, colors and error message as Input.
function TextArea({
  label,
  name,
  value,
  onChange,
  error,
  disabled = false,
  rows = 4,
  maxLength,
}: TextAreaProps) {
  return (
    <div className="field">
      <label className="field-label" htmlFor={name}>
        {label}
      </label>
      <textarea
        className={error ? 'field-input field-textarea field-input-error' : 'field-input field-textarea'}
        id={name}
        name={name}
        value={value}
        rows={rows}
        maxLength={maxLength}
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

export default TextArea
