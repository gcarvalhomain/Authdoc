import { useState, type SubmitEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import { register } from '../api/auth.ts'
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

type RegisterField = 'name' | 'email' | 'password' | 'confirmationPassword'

// The validation codes of POST /api/auth/register (see "Error format" in docs/api.md).
const fieldByCode: FieldByCode<RegisterField> = {
  NAME_REQUIRED: 'name',
  NAME_TOO_LONG: 'name',
  EMAIL_REQUIRED: 'email',
  EMAIL_INVALID: 'email',
  EMAIL_ALREADY_IN_USE: 'email',
  PASSWORD_REQUIRED: 'password',
  PASSWORD_TOO_SHORT: 'password',
  PASSWORD_TOO_LONG: 'password',
  PASSWORDS_DO_NOT_MATCH: 'confirmationPassword',
}

function RegisterPage() {
  const navigate = useNavigate()
  const { auth } = useAuth()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmationPassword, setConfirmationPassword] = useState('')
  const [errors, setErrors] = useState<FormErrors<RegisterField>>(emptyFormErrors)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setErrors(emptyFormErrors())
    setIsSubmitting(true)

    try {
      const user = await register({ name, email, password, confirmationPassword })
      // The sign-in page reads this state: it shows a success message
      // and fills in the email. The API answers with the email already normalized.
      navigate('/login', { state: { registeredEmail: user.email } })
    } catch (caught) {
      setErrors(toFormErrors(caught, fieldByCode))
      setIsSubmitting(false)
    }
  }

  // A signed-in user does not need a new account.
  if (auth.status === 'signedIn') {
    return <Navigate to="/account" replace />
  }

  return (
    <PageLayout>
      <div className="auth-card">
        <h1>Create account</h1>
        <p className="auth-subtitle">Keep your documents in one place.</p>

        {errors.form && <Alert variant="error">{errors.form}</Alert>}

        <form className="auth-form" onSubmit={handleSubmit} noValidate>
          <Input
            label="Name"
            name="name"
            autoComplete="name"
            value={name}
            onChange={setName}
            error={errors.fields.name}
            disabled={isSubmitting}
          />
          <Input
            label="Email"
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={setEmail}
            error={errors.fields.email}
            disabled={isSubmitting}
          />
          <Input
            label="Password"
            name="password"
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={setPassword}
            error={errors.fields.password}
            disabled={isSubmitting}
          />
          <Input
            label="Confirm password"
            name="confirmationPassword"
            type="password"
            autoComplete="new-password"
            value={confirmationPassword}
            onChange={setConfirmationPassword}
            error={errors.fields.confirmationPassword}
            disabled={isSubmitting}
          />
          <Button type="submit" variant="primary" disabled={isSubmitting}>
            {isSubmitting ? 'Creating account...' : 'Create account'}
          </Button>
        </form>

        <p className="auth-footer">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </div>
    </PageLayout>
  )
}

export default RegisterPage
