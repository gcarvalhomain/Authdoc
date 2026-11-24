import { startTransition, useState, type SubmitEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { changePassword } from '../api/auth.ts'
import {
  emptyFormErrors,
  toFormErrors,
  type FieldByCode,
  type FormErrors,
} from '../api/formErrors.ts'
import { useAuth } from '../auth/AuthContext.ts'
import Alert from '../shared/Alert.tsx'
import Button from '../shared/Button.tsx'
import Input from '../shared/Input.tsx'
import PageLayout from '../shared/PageLayout.tsx'
import './AuthPage.css'

type PasswordField = 'currentPassword' | 'newPassword' | 'confirmationPassword'

// The validation codes of PUT /api/auth/password (see "Error format" in docs/api.md).
// Any other code (too many attempts, no connection...) shows at the top of the form.
const fieldByCode: FieldByCode<PasswordField> = {
  CURRENT_PASSWORD_REQUIRED: 'currentPassword',
  CURRENT_PASSWORD_INCORRECT: 'currentPassword',
  PASSWORD_REQUIRED: 'newPassword',
  PASSWORD_TOO_SHORT: 'newPassword',
  PASSWORD_TOO_LONG: 'newPassword',
  NEW_PASSWORD_SAME_AS_CURRENT: 'newPassword',
  PASSWORDS_DO_NOT_MATCH: 'confirmationPassword',
}

// What this page sends to the sign-in page after a success.
export type PasswordChangedState = { passwordChanged: true }

function ChangePasswordPage() {
  const navigate = useNavigate()
  const { signOut } = useAuth()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmationPassword, setConfirmationPassword] = useState('')
  const [errors, setErrors] = useState<FormErrors<PasswordField>>(emptyFormErrors)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setErrors(emptyFormErrors())
    setIsSubmitting(true)

    try {
      await changePassword({ currentPassword, newPassword, confirmationPassword })
    } catch (caught) {
      setErrors(toFormErrors(caught, fieldByCode))
      setIsSubmitting(false)
      return
    }

    // The API has ended every session of the user, this one too, so the tokens
    // on this device do not work anymore. The app forgets them and asks for a
    // new sign-in with the new password.
    const state: PasswordChangedState = { passwordChanged: true }
    // React Router changes the address inside a transition (a low-priority update).
    // signOut goes in the same transition, so React draws both together. Without it,
    // React draws "signed out" first, on this page, and RequireAuth sends its own
    // redirect to /login, which loses the passwordChanged message.
    startTransition(() => {
      navigate('/login', { replace: true, state })
      signOut()
    })
  }

  return (
    <PageLayout>
      <div className="auth-card">
        <h1>Change password</h1>
        <p className="auth-subtitle">
          After the change, you need to sign in again on all your devices.
        </p>

        {errors.form && <Alert variant="error">{errors.form}</Alert>}

        <form className="auth-form" onSubmit={handleSubmit} noValidate>
          <Input
            label="Current password"
            name="currentPassword"
            type="password"
            autoComplete="current-password"
            value={currentPassword}
            onChange={setCurrentPassword}
            error={errors.fields.currentPassword}
            disabled={isSubmitting}
          />
          <Input
            label="New password"
            name="newPassword"
            type="password"
            autoComplete="new-password"
            value={newPassword}
            onChange={setNewPassword}
            error={errors.fields.newPassword}
            disabled={isSubmitting}
          />
          <Input
            label="Confirm new password"
            name="confirmationPassword"
            type="password"
            autoComplete="new-password"
            value={confirmationPassword}
            onChange={setConfirmationPassword}
            error={errors.fields.confirmationPassword}
            disabled={isSubmitting}
          />
          <Button type="submit" variant="primary" disabled={isSubmitting}>
            {isSubmitting ? 'Changing password...' : 'Change password'}
          </Button>
        </form>

        <p className="auth-footer">
          <Link to="/account">Back to my account</Link>
        </p>
      </div>
    </PageLayout>
  )
}

export default ChangePasswordPage
