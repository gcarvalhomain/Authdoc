import { useEffect, useRef, useState } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router'
import { ApiError } from '../api/client.ts'
import { deleteDocument, documentsPageSize, listDocuments } from '../api/documents.ts'
import { getErrorMessage } from '../api/errorMessages.ts'
import type { DocumentResponse, PagedResponse } from '../api/types.ts'
import Alert from '../shared/Alert.tsx'
import Button from '../shared/Button.tsx'
import PageLayout from '../shared/PageLayout.tsx'
import '../shared/Button.css'
import {
  readMessage,
  type DocumentFormState,
  type DocumentsListState,
} from './documentHelpers.ts'
import './DocumentsPage.css'

// Reads the page number from the address (?page=2).
// A missing or wrong value ("abc", "0", "-1", "2.5") means page 1.
function readPage(value: string | null): number {
  const page = Number(value)
  return Number.isInteger(page) && page >= 1 ? page : 1
}

// Formats a date without a time, like "2026-11-15" (DateOnly in C#).
// JavaScript reads that text as midnight in UTC. Shown in the local time zone,
// it would be the day before in Brazil (UTC-3), so the format uses UTC too.
function formatDateOnly(value: string): string {
  return new Date(value).toLocaleDateString('en-US', { dateStyle: 'medium', timeZone: 'UTC' })
}

// The answer of one request: the page and the attempt it belongs to.
type ListResult =
  | { page: number; attempt: number; status: 'loaded'; data: PagedResponse<DocumentResponse> }
  | { page: number; attempt: number; status: 'error'; message: string }

function DocumentsPage() {
  // useSearchParams works like useState, but the value lives in the address.
  // So a reload, the Back button and a shared link all keep the same page.
  const [searchParams, setSearchParams] = useSearchParams()
  const page = readPage(searchParams.get('page'))

  // The form sends a message here after a save, like "Document added.".
  // A click on Previous or Next opens a new address without it, so it goes away.
  const location = useLocation()
  const message = readMessage(location.state)
  // The form goes back to this exact list page after Save or Cancel.
  const formState: DocumentFormState = { returnTo: location.pathname + location.search }

  // "Try again" adds 1 here. The number is in the effect's list below,
  // so a new number sends the same request once more.
  const [attempt, setAttempt] = useState(0)
  const [result, setResult] = useState<ListResult | null>(null)

  // [page, attempt]: the effect runs again when the page in the address changes
  // or when the user clicks "Try again".
  useEffect(() => {
    // If the page changes before the answer arrives, ignore the old answer.
    let ignore = false
    listDocuments(page, documentsPageSize)
      .then((data) => {
        if (!ignore) {
          setResult({ page, attempt, status: 'loaded', data })
        }
      })
      .catch((caught: unknown) => {
        if (!ignore) {
          setResult({ page, attempt, status: 'error', message: getErrorMessage(caught) })
        }
      })

    return () => {
      ignore = true
    }
  }, [page, attempt])

  // The delete has two clicks: "Delete" opens a question in the row (confirmingId),
  // and the second "Delete" sends the call (deletingId while it waits).
  const [confirmingId, setConfirmingId] = useState<string | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  // False after the user leaves the page. A delete answer that arrives later
  // must not bring the user back to the list.
  const isMounted = useRef(true)
  useEffect(() => {
    isMounted.current = true
    return () => {
      isMounted.current = false
    }
  }, [])

  function goToPage(newPage: number, state?: DocumentsListState) {
    // A question or an error of the old page does not belong to the new one.
    setConfirmingId(null)
    setDeleteError(null)
    // Page 1 is the default, so the address stays clean: /documents.
    // replace + state: after a delete, the message goes with the address,
    // and the Back button does not show the deleted document again.
    setSearchParams(newPage === 1 ? {} : { page: String(newPage) }, state && { replace: true, state })
  }

  async function handleDelete(document: DocumentResponse, itemsOnPage: number) {
    setDeleteError(null)
    setDeletingId(document.id)

    let message = 'Document deleted.'
    try {
      await deleteDocument(document.id)
    } catch (caught) {
      // Already deleted (for example, in another tab): the goal is reached,
      // so the list only loads again. Any other error stays on this page.
      if (!(caught instanceof ApiError) || caught.code !== 'DOCUMENT_NOT_FOUND') {
        if (isMounted.current) {
          setDeleteError(getErrorMessage(caught))
          setDeletingId(null)
          setConfirmingId(null)
        }
        return
      }
      message = 'This document was already deleted.'
    }

    if (!isMounted.current) {
      return
    }

    setDeletingId(null)
    // The last document of a page leaves that page empty, so the list shows
    // the page before it. Page 1 stays page 1 (it shows "no documents yet").
    const newPage = itemsOnPage === 1 && page > 1 ? page - 1 : page
    goToPage(newPage, { message })
    // The address can stay the same (same page), so a new attempt makes the
    // list load again from the API.
    setAttempt((value) => value + 1)
  }

  // A result of another page or another attempt is old: the answer for this one
  // is on its way. So "loading" is not a separate state: it is "no current result".
  const current = result?.page === page && result.attempt === attempt ? result : null

  return (
    <PageLayout>
      <div className="documents-header">
        <h1>My documents</h1>
        <Link className="button button-primary" to="/documents/new" state={formState}>
          Add document
        </Link>
      </div>

      {message && !deleteError && <Alert variant="success">{message}</Alert>}
      {deleteError && <Alert variant="error">{deleteError}</Alert>}

      {!current && <p className="documents-message">Loading documents...</p>}

      {current?.status === 'error' && (
        <div className="documents-error">
          <Alert variant="error">{current.message}</Alert>
          <Button onClick={() => setAttempt((value) => value + 1)}>Try again</Button>
        </div>
      )}

      {current?.status === 'loaded' && (
        <DocumentsList
          data={current.data}
          formState={formState}
          onPageChange={goToPage}
          deleteState={{ confirmingId, deletingId }}
          onAskDelete={(id) => {
            setDeleteError(null)
            setConfirmingId(id)
          }}
          onCancelDelete={() => setConfirmingId(null)}
          onConfirmDelete={(document) => handleDelete(document, current.data.items.length)}
        />
      )}
    </PageLayout>
  )
}

type DeleteState = {
  confirmingId: string | null
  deletingId: string | null
}

type DocumentsListProps = {
  data: PagedResponse<DocumentResponse>
  formState: DocumentFormState
  onPageChange: (page: number) => void
  deleteState: DeleteState
  onAskDelete: (id: string) => void
  onCancelDelete: () => void
  onConfirmDelete: (document: DocumentResponse) => void
}

// The part of the page after the answer arrives: the table, or a message when
// there is nothing to show.
function DocumentsList({
  data,
  formState,
  onPageChange,
  deleteState,
  onAskDelete,
  onCancelDelete,
  onConfirmDelete,
}: DocumentsListProps) {
  // The user has no documents at all.
  if (data.totalCount === 0) {
    return <p className="documents-message">You have no documents yet.</p>
  }

  // The address asks for a page after the last one, for example ?page=99,
  // from an old link or after documents were deleted.
  if (data.items.length === 0) {
    return (
      <div className="documents-message">
        <p>This page has no documents.</p>
        <Button onClick={() => onPageChange(1)}>Go to page 1</Button>
      </div>
    )
  }

  return (
    <>
      {/* On a small screen the table scrolls to the side inside this box. */}
      <div className="documents-table-wrapper">
        <table className="documents-table">
          <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Type</th>
              <th scope="col">Number</th>
              <th scope="col">Issued</th>
              <th scope="col">Expires</th>
              {/* The column of the Edit and Delete actions has a name for screen readers only. */}
              <th scope="col">
                <span className="visually-hidden">Actions</span>
              </th>
            </tr>
          </thead>
          <tbody>
            {/* key tells React which row is which when the list changes. */}
            {data.items.map((document) => (
              <tr key={document.id}>
                <td>{document.name}</td>
                <td>{document.type}</td>
                <td className="documents-number">{document.number}</td>
                <td>{formatDateOnly(document.issuedAt)}</td>
                <td>{document.expiresAt ? formatDateOnly(document.expiresAt) : 'Never'}</td>
                <td className="documents-actions">
                  <DocumentActions
                    document={document}
                    formState={formState}
                    deleteState={deleteState}
                    onAskDelete={onAskDelete}
                    onCancelDelete={onCancelDelete}
                    onConfirmDelete={onConfirmDelete}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* With one page only, the buttons would do nothing, so they are hidden. */}
      {data.totalPages > 1 && (
        <nav className="documents-pagination" aria-label="Pages">
          <Button disabled={data.page <= 1} onClick={() => onPageChange(data.page - 1)}>
            Previous
          </Button>
          <span>
            Page {data.page} of {data.totalPages}
          </span>
          <Button
            disabled={data.page >= data.totalPages}
            onClick={() => onPageChange(data.page + 1)}
          >
            Next
          </Button>
        </nav>
      )}
    </>
  )
}

type DocumentActionsProps = {
  document: DocumentResponse
  formState: DocumentFormState
  deleteState: DeleteState
  onAskDelete: (id: string) => void
  onCancelDelete: () => void
  onConfirmDelete: (document: DocumentResponse) => void
}

// The last cell of a row: "Edit" and "Delete", or the delete question.
function DocumentActions({
  document,
  formState,
  deleteState,
  onAskDelete,
  onCancelDelete,
  onConfirmDelete,
}: DocumentActionsProps) {
  const isDeleting = deleteState.deletingId === document.id

  if (deleteState.confirmingId === document.id) {
    return (
      // role="group" + aria-label: a screen reader reads the question with its buttons.
      <span className="documents-confirm" role="group" aria-label={`Delete ${document.name}?`}>
        <span>Delete this document?</span>
        <Button
          variant="danger"
          disabled={isDeleting}
          ariaLabel={`Yes, delete ${document.name}`}
          onClick={() => onConfirmDelete(document)}
        >
          {isDeleting ? 'Deleting...' : 'Delete'}
        </Button>
        {/* autoFocus: the keyboard focus moves here, the safe choice, when the question opens. */}
        <Button disabled={isDeleting} autoFocus onClick={onCancelDelete}>
          Cancel
        </Button>
      </span>
    )
  }

  return (
    <>
      {/* aria-label: a screen reader says "Edit Passport", not only "Edit". */}
      <Link
        to={`/documents/${document.id}/edit`}
        state={formState}
        aria-label={`Edit ${document.name}`}
      >
        Edit
      </Link>
      {/* While another document is being deleted, the other Delete buttons wait. */}
      <button
        type="button"
        className="documents-delete"
        aria-label={`Delete ${document.name}`}
        disabled={deleteState.deletingId !== null}
        onClick={() => onAskDelete(document.id)}
      >
        Delete
      </button>
    </>
  )
}

export default DocumentsPage
