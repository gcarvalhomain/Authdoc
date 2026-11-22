import { useEffect, useState } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router'
import { documentsPageSize, listDocuments } from '../api/documents.ts'
import { getErrorMessage } from '../api/errorMessages.ts'
import type { DocumentResponse, PagedResponse } from '../api/types.ts'
import Alert from '../shared/Alert.tsx'
import Button from '../shared/Button.tsx'
import PageLayout from '../shared/PageLayout.tsx'
import '../shared/Button.css'
import { readMessage, type DocumentFormState } from './documentHelpers.ts'
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

  function goToPage(newPage: number) {
    // Page 1 is the default, so the address stays clean: /documents.
    setSearchParams(newPage === 1 ? {} : { page: String(newPage) })
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

      {message && <Alert variant="success">{message}</Alert>}

      {!current && <p className="documents-message">Loading documents...</p>}

      {current?.status === 'error' && (
        <div className="documents-error">
          <Alert variant="error">{current.message}</Alert>
          <Button onClick={() => setAttempt((value) => value + 1)}>Try again</Button>
        </div>
      )}

      {current?.status === 'loaded' && (
        <DocumentsList data={current.data} formState={formState} onPageChange={goToPage} />
      )}
    </PageLayout>
  )
}

type DocumentsListProps = {
  data: PagedResponse<DocumentResponse>
  formState: DocumentFormState
  onPageChange: (page: number) => void
}

// The part of the page after the answer arrives: the table, or a message when
// there is nothing to show.
function DocumentsList({ data, formState, onPageChange }: DocumentsListProps) {
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
              {/* The column of the Edit links has a name for screen readers only. */}
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
                  {/* aria-label: a screen reader says "Edit Passport", not only "Edit". */}
                  <Link
                    to={`/documents/${document.id}/edit`}
                    state={formState}
                    aria-label={`Edit ${document.name}`}
                  >
                    Edit
                  </Link>
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

export default DocumentsPage
