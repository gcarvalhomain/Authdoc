import type { ReactNode } from 'react'
import './Button.css'

type ButtonProps = {
  children: ReactNode
  variant?: 'primary' | 'secondary'
  type?: 'button' | 'submit'
  disabled?: boolean
  onClick?: () => void
}

function Button({
  children,
  variant = 'secondary',
  type = 'button',
  disabled = false,
  onClick,
}: ButtonProps) {
  return (
    <button
      className={`button button-${variant}`}
      type={type}
      disabled={disabled}
      onClick={onClick}
    >
      {children}
    </button>
  )
}

export default Button
