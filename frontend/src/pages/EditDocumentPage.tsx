import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import { getDocument, updateDocument } from '../api/documents.ts'
import { getErrorMessage } from '../api/errorMessages.ts'
import type { DocumentRequest, DocumentResponse } from '../api/types.ts'
import Alert from '../shared/Alert.tsx'
import PageLayout from '../shared/PageLayout.tsx'
import DocumentForm from './DocumentForm.tsx'
import {
  readReturnTo,
  toFormValues,
  type DocumentsListState,
} from './documentHelpers.ts'

// The answer of the request that loads the document, with the id it belongs to.
type LoadResult =
  | { id: string; status: 'loaded'; document: DocumentResponse }
  | { id: string; status: 'error'; message: string }

function EditDocumentPage() {
  // useParams reads the parts of the address: for /documents/123/edit, id is "123".
  // The route always has an id, so "?? ''" only satisfies TypeScript.
  const id = useParams().id ?? ''
  const navigate = useNavigate()
  const location = useLocation()
  const returnTo = readReturnTo(location.state)

  const [result, setResult] = useState<LoadResult | null>(null)

  useEffect(() => {
    // If the id changes before the answer arrives, ignore the old answer.
    let ignore = false
    getDocument(id)
      .then((document) => {
        if (!ignore) {
          setResult({ id, status: 'loaded', document })
        }
      })
      .catch((caught: unknown) => {
        if (!ignore) {
          setResult({ id, status: 'error', message: getErrorMessage(caught) })
        }
      })

    return () => {
      ignore = true
    }
  }, [id])

  async function handleSave(request: DocumentRequest) {
    // If this call fails, the error goes back to the form.
    await updateDocument(id, request)
    const state: DocumentsListState = { message: 'Document updated.' }
    navigate(returnTo, { state })
  }

  const current = result?.id === id ? result : null

  return (
    <PageLayout>
      <h1>Edit document</h1>

      {!current && <p>Loading document...</p>}

      {current?.status === 'error' && (
        <>
          <Alert variant="error">{current.message}</Alert>
          <p>
            <Link to={returnTo}>Back to my documents</Link>
          </p>
        </>
      )}

      {current?.status === 'loaded' && (
        <div className="document-form-card">
          {/* key: a new document gets a new form, with its own values. */}
          <DocumentForm
            key={current.document.id}
            initialValues={toFormValues(current.document)}
            submitLabel="Save changes"
            submittingLabel="Saving..."
            onSave={handleSave}
            onCancel={() => navigate(returnTo)}
          />
        </div>
      )}
    </PageLayout>
  )
}

export default EditDocumentPage
