import { useLocation, useNavigate } from 'react-router'
import { createDocument, documentsPageSize, listDocuments } from '../api/documents.ts'
import type { DocumentRequest } from '../api/types.ts'
import PageLayout from '../shared/PageLayout.tsx'
import DocumentForm from './DocumentForm.tsx'
import {
  emptyDocumentValues,
  readReturnTo,
  type DocumentsListState,
} from './documentHelpers.ts'

function NewDocumentPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const returnTo = readReturnTo(location.state)

  async function handleSave(request: DocumentRequest) {
    // If this call fails, the error goes back to the form.
    await createDocument(request)

    // The list shows the oldest documents first, so the new one is on the
    // last page. A list with 1 item per page tells how many documents exist.
    let target = '/documents'
    try {
      const { totalCount } = await listDocuments(1, 1)
      const lastPage = Math.ceil(totalCount / documentsPageSize)
      if (lastPage > 1) {
        target = `/documents?page=${lastPage}`
      }
    } catch {
      // The document is already saved. Page 1 is fine in this case.
    }

    const state: DocumentsListState = { message: 'Document added.' }
    navigate(target, { state })
  }

  return (
    <PageLayout>
      <h1>Add document</h1>
      <div className="document-form-card">
        <DocumentForm
          initialValues={emptyDocumentValues}
          submitLabel="Add document"
          submittingLabel="Adding..."
          onSave={handleSave}
          onCancel={() => navigate(returnTo)}
        />
      </div>
    </PageLayout>
  )
}

export default NewDocumentPage
