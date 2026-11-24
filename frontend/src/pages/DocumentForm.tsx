import { useState, type SubmitEvent } from 'react'
import {
  emptyFormErrors,
  toFormErrors,
  type FieldByCode,
  type FormErrors,
} from '../api/formErrors.ts'
import type { DocumentRequest } from '../api/types.ts'
import Alert from '../shared/Alert.tsx'
import Button from '../shared/Button.tsx'
import Input from '../shared/Input.tsx'
import TextArea from '../shared/TextArea.tsx'
import { documentLimits, toDocumentRequest, type DocumentFormValues } from './documentHelpers.ts'
import './DocumentForm.css'

type DocumentField = keyof DocumentFormValues

// The validation codes of POST and PUT /api/documents (see "Error format" in
// docs/api.md), and the field that shows each one. Any other error, like no connection
// or DOCUMENT_NOT_FOUND, shows at the top of the form.
const fieldByCode: FieldByCode<DocumentField> = {
  TYPE_REQUIRED: 'type',
  TYPE_TOO_LONG: 'type',
  NAME_REQUIRED: 'name',
  NAME_TOO_LONG: 'name',
  NUMBER_REQUIRED: 'number',
  NUMBER_TOO_LONG: 'number',
  DESCRIPTION_TOO_LONG: 'description',
  ISSUED_AT_REQUIRED: 'issuedAt',
  ISSUED_AT_IN_FUTURE: 'issuedAt',
  // The rule compares two dates, but the expiry date is the one to fix.
  EXPIRES_AT_BEFORE_ISSUED_AT: 'expiresAt',
}

// Suggestions for the Type field. The API accepts any text, so the user can
// also type a type that is not in this list.
const typeSuggestions = [
  'ID card',
  'Passport',
  "Driver's license",
  'Birth certificate',
  'Marriage certificate',
  'Work permit',
  'Vehicle registration',
  'Health insurance card',
]

type DocumentFormProps = {
  initialValues: DocumentFormValues
  submitLabel: string
  submittingLabel: string
  // The page decides what Save does (create or update) and where to go after it.
  // If it throws, the form shows the error and the user can try again.
  onSave: (request: DocumentRequest) => Promise<void>
  onCancel: () => void
}

// The form of a document. The new page and the edit page both use it:
// the fields, the errors and the buttons are the same, only Save is different.
function DocumentForm({
  initialValues,
  submitLabel,
  submittingLabel,
  onSave,
  onCancel,
}: DocumentFormProps) {
  // One state for all the fields, because they are saved together.
  const [values, setValues] = useState(initialValues)
  const [errors, setErrors] = useState<FormErrors<DocumentField>>(emptyFormErrors)
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Returns the onChange of one field. It copies the old values ({ ...current })
  // and changes only this field: React needs a new object to see a change.
  function changeField(field: DocumentField) {
    return (value: string) => setValues((current) => ({ ...current, [field]: value }))
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setErrors(emptyFormErrors())
    setIsSubmitting(true)

    try {
      await onSave(toDocumentRequest(values))
      // It worked: the page has already moved to another address,
      // so the form does not change its state anymore.
    } catch (caught) {
      setErrors(toFormErrors(caught, fieldByCode))
      setIsSubmitting(false)
    }
  }

  return (
    <form className="document-form" onSubmit={handleSubmit} noValidate>
      {errors.form && <Alert variant="error">{errors.form}</Alert>}

      <Input
        label="Type"
        name="type"
        list="document-type-suggestions"
        maxLength={documentLimits.type}
        value={values.type}
        onChange={changeField('type')}
        error={errors.fields.type}
        disabled={isSubmitting}
      />
      <datalist id="document-type-suggestions">
        {typeSuggestions.map((suggestion) => (
          <option key={suggestion} value={suggestion} />
        ))}
      </datalist>

      <Input
        label="Name"
        name="name"
        maxLength={documentLimits.name}
        value={values.name}
        onChange={changeField('name')}
        error={errors.fields.name}
        disabled={isSubmitting}
      />
      <Input
        label="Number"
        name="number"
        maxLength={documentLimits.number}
        value={values.number}
        onChange={changeField('number')}
        error={errors.fields.number}
        disabled={isSubmitting}
      />

      {/* The two dates side by side on a wide screen. */}
      <div className="document-form-dates">
        <Input
          label="Issue date"
          name="issuedAt"
          type="date"
          value={values.issuedAt}
          onChange={changeField('issuedAt')}
          error={errors.fields.issuedAt}
          disabled={isSubmitting}
        />
        <Input
          label="Expiry date (optional)"
          name="expiresAt"
          type="date"
          value={values.expiresAt}
          onChange={changeField('expiresAt')}
          error={errors.fields.expiresAt}
          disabled={isSubmitting}
        />
      </div>

      <TextArea
        label="Description (optional)"
        name="description"
        maxLength={documentLimits.description}
        value={values.description}
        onChange={changeField('description')}
        error={errors.fields.description}
        disabled={isSubmitting}
      />

      <div className="document-form-actions">
        <Button type="submit" variant="primary" disabled={isSubmitting}>
          {isSubmitting ? submittingLabel : submitLabel}
        </Button>
        <Button onClick={onCancel} disabled={isSubmitting}>
          Cancel
        </Button>
      </div>
    </form>
  )
}

export default DocumentForm
