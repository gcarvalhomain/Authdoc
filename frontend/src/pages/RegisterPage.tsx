import { useState, type SubmitEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { register } from '../api/auth.ts'
import { getErrorMessage } from '../api/errorMessages.ts'
import Alert from '../shared/Alert.tsx'
import Button from '../shared/Button.tsx'
import Input from '../shared/Input.tsx'
import PageLayout from '../shared/PageLayout.tsx'
import './AuthPage.css'

function RegisterPage() {
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmationPassword, setConfirmationPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const user = await register({ name, email, password, confirmationPassword })
      // The sign-in page reads this state: it shows a success message
      // and fills in the email. The API answers with the email already normalized.
      navigate('/login', { state: { registeredEmail: user.email } })
    } catch (caught) {
      setError(getErrorMessage(caught))
      setIsSubmitting(false)
    }
  }

  return (
    <PageLayout>
      <div className="auth-card">
        <h1>Create account</h1>
        <p className="auth-subtitle">Keep your documents in one place.</p>

        {error && <Alert variant="error">{error}</Alert>}

        <form className="auth-form" onSubmit={handleSubmit} noValidate>
          <Input
            label="Name"
            name="name"
            autoComplete="name"
            value={name}
            onChange={setName}
            disabled={isSubmitting}
          />
          <Input
            label="Email"
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={setEmail}
            disabled={isSubmitting}
          />
          <Input
            label="Password"
            name="password"
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={setPassword}
            disabled={isSubmitting}
          />
          <Input
            label="Confirm password"
            name="confirmationPassword"
            type="password"
            autoComplete="new-password"
            value={confirmationPassword}
            onChange={setConfirmationPassword}
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
