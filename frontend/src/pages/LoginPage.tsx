import { useState, type SubmitEvent } from 'react'
import { Link, useLocation } from 'react-router'
import { login } from '../api/auth.ts'
import {
  emptyFormErrors,
  hasFormErrors,
  toFormErrors,
  type FieldByCode,
  type FormErrors,
} from '../api/formErrors.ts'
import type { UserResponse } from '../api/types.ts'
import Alert from '../shared/Alert.tsx'
import Button from '../shared/Button.tsx'
import Input from '../shared/Input.tsx'
import PageLayout from '../shared/PageLayout.tsx'
import './AuthPage.css'

type LoginField = 'email' | 'password'

// A wrong email or password is INVALID_CREDENTIALS, which is not in this map, so it
// shows at the top of the form: the page must not tell which of the two is wrong.
const fieldByCode: FieldByCode<LoginField> = {
  EMAIL_REQUIRED: 'email',
  PASSWORD_REQUIRED: 'password',
}

// The sign-up page sends { registeredEmail } after it creates an account.
// The state can be anything (or nothing), so each part is checked before it is used.
function readRegisteredEmail(state: unknown): string | null {
  if (
    typeof state === 'object' &&
    state !== null &&
    'registeredEmail' in state &&
    typeof state.registeredEmail === 'string'
  ) {
    return state.registeredEmail
  }

  return null
}

function LoginPage() {
  const location = useLocation()
  const registeredEmail = readRegisteredEmail(location.state)

  // Each useState keeps one value between renders, like a field of a class in C#.
  // Calling the "set" function changes the value and draws the page again.
  const [email, setEmail] = useState(registeredEmail ?? '')
  const [password, setPassword] = useState('')
  const [errors, setErrors] = useState<FormErrors<LoginField>>(emptyFormErrors)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [signedInUser, setSignedInUser] = useState<UserResponse | null>(null)

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    // Without this, the browser would reload the page to send the form.
    event.preventDefault()
    setErrors(emptyFormErrors())
    setIsSubmitting(true)

    try {
      const response = await login({ email, password })
      setSignedInUser(response.user)
    } catch (caught) {
      setErrors(toFormErrors(caught, fieldByCode))
    } finally {
      setIsSubmitting(false)
    }
  }

  // Temporary: step 5 saves the session and opens the logged-in area here.
  if (signedInUser) {
    return (
      <PageLayout>
        <div className="auth-card">
          <h1>Signed in</h1>
          <Alert variant="success">You are signed in as {signedInUser.name}.</Alert>
        </div>
      </PageLayout>
    )
  }

  return (
    <PageLayout>
      <div className="auth-card">
        <h1>Sign in</h1>
        <p className="auth-subtitle">Access your documents.</p>

        {errors.form && <Alert variant="error">{errors.form}</Alert>}
        {!hasFormErrors(errors) && registeredEmail && (
          <Alert variant="success">Your account was created. Sign in to continue.</Alert>
        )}

        {/* noValidate turns off the browser checks, so the messages come
            from the API and look the same in every browser. */}
        <form className="auth-form" onSubmit={handleSubmit} noValidate>
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
            autoComplete="current-password"
            value={password}
            onChange={setPassword}
            error={errors.fields.password}
            disabled={isSubmitting}
          />
          <Button type="submit" variant="primary" disabled={isSubmitting}>
            {isSubmitting ? 'Signing in...' : 'Sign in'}
          </Button>
        </form>

        <p className="auth-footer">
          Don't have an account? <Link to="/register">Create one</Link>
        </p>
      </div>
    </PageLayout>
  )
}

export default LoginPage
