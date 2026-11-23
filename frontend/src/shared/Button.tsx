import type { ReactNode } from 'react'
import './Button.css'

type ButtonProps = {
  children: ReactNode
  // danger: for an action that cannot be undone, like a delete.
  variant?: 'primary' | 'secondary' | 'danger'
  type?: 'button' | 'submit'
  disabled?: boolean
  // Puts the keyboard focus on the button when it appears.
  autoFocus?: boolean
  // What a screen reader says, when the text alone is not clear ("Delete" what?).
  ariaLabel?: string
  onClick?: () => void
}

function Button({
  children,
  variant = 'secondary',
  type = 'button',
  disabled = false,
  autoFocus = false,
  ariaLabel,
  onClick,
}: ButtonProps) {
  return (
    <button
      className={`button button-${variant}`}
      type={type}
      disabled={disabled}
      autoFocus={autoFocus}
      aria-label={ariaLabel}
      onClick={onClick}
    >
      {children}
    </button>
  )
}

export default Button
