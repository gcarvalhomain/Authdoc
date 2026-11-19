import type { ReactNode } from 'react'
import './Alert.css'

type AlertProps = {
  children: ReactNode
  variant: 'error' | 'success'
}

// A message box above a form: an error from the API or a success message.
function Alert({ children, variant }: AlertProps) {
  return (
    // A screen reader reads role="alert" at once, and role="status" when the user is not busy.
    <div className={`alert alert-${variant}`} role={variant === 'error' ? 'alert' : 'status'}>
      {children}
    </div>
  )
}

export default Alert
