import { ApiError } from './client.ts'
import { getErrorMessage } from './errorMessages.ts'

// Which field of a form an API error code belongs to. Each form has its own map,
// because the same code can belong to a different field in another form.
// F is the list of field names, for example 'email' | 'password'.
export type FieldByCode<F extends string> = Partial<Record<string, F>>

// The errors that a form shows: one message under a field, or one at the top.
export type FormErrors<F extends string> = {
  fields: Partial<Record<F, string>>
  form: string | null
}

export function emptyFormErrors<F extends string>(): FormErrors<F> {
  return { fields: {}, form: null }
}

export function hasFormErrors<F extends string>(errors: FormErrors<F>): boolean {
  return errors.form !== null || Object.keys(errors.fields).length > 0
}

// Turns an error caught in a form into the place and the text to show.
// A code in the map goes under its field. Any other error goes to the top
// of the form: a wrong password, no connection, too many attempts, and so on.
export function toFormErrors<F extends string>(
  error: unknown,
  fieldByCode: FieldByCode<F>,
): FormErrors<F> {
  const message = getErrorMessage(error)

  // Object.hasOwn ignores names that every object has, like "toString".
  if (error instanceof ApiError && Object.hasOwn(fieldByCode, error.code)) {
    const field = fieldByCode[error.code] as F
    const fields: Partial<Record<F, string>> = {}
    fields[field] = message
    return { fields, form: null }
  }

  return { fields: {}, form: message }
}
