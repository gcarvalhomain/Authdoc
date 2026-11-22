import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router'
import { listDocuments } from '../api/documents.ts'
import { getErrorMessage } from '../api/errorMessages.ts'
import type { DocumentResponse, PagedResponse } from '../api/types.ts'
import Alert from '../shared/Alert.tsx'
import Button from '../shared/Button.tsx'
import PageLayout from '../shared/PageLayout.tsx'
import './DocumentsPage.css'

const pageSize = 10

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

// The answer of one request, with the page it belongs to.
type ListResult =
  | { page: number; status: 'loaded'; data: PagedResponse<DocumentResponse> }
  | { page: number; status: 'error'; message: string }

function DocumentsPage() {
  // useSearchParams works like useState, but the value lives in the address.
  // So a reload, the Back button and a shared link all keep the same page.
  const [searchParams, setSearchParams] = useSearchParams()
  const page = readPage(searchParams.get('page'))

  const [result, setResult] = useState<ListResult | null>(null)

  // [page]: the effect runs again every time the page in the address changes.
  useEffect(() => {
    // If the page changes before the answer arrives, ignore the old answer.
    let ignore = false
    listDocuments(page, pageSize)
      .then((data) => {
        if (!ignore) {
          setResult({ page, status: 'loaded', data })
        }
      })
      .catch((caught: unknown) => {
        if (!ignore) {
          setResult({ page, status: 'error', message: getErrorMessage(caught) })
        }
      })

    return () => {
      ignore = true
    }
  }, [page])

  function goToPage(newPage: number) {
    // Page 1 is the default, so the address stays clean: /documents.
    setSearchParams(newPage === 1 ? {} : { page: String(newPage) })
  }

  // The result of another page is old: the answer for this page is on its way.
  const current = result?.page === page ? result : null

  return (
    <PageLayout>
      <h1>My documents</h1>

      {!current && <p>Loading...</p>}
      {current?.status === 'error' && <Alert variant="error">{current.message}</Alert>}

      {current?.status === 'loaded' && (
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
                </tr>
              </thead>
              <tbody>
                {/* key tells React which row is which when the list changes. */}
                {current.data.items.map((document) => (
                  <tr key={document.id}>
                    <td>{document.name}</td>
                    <td>{document.type}</td>
                    <td className="documents-number">{document.number}</td>
                    <td>{formatDateOnly(document.issuedAt)}</td>
                    <td>{document.expiresAt ? formatDateOnly(document.expiresAt) : 'Never'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <nav className="documents-pagination" aria-label="Pages">
            <Button disabled={page <= 1} onClick={() => goToPage(page - 1)}>
              Previous
            </Button>
            <span>
              Page {page} of {current.data.totalPages}
            </span>
            <Button
              disabled={page >= current.data.totalPages}
              onClick={() => goToPage(page + 1)}
            >
              Next
            </Button>
          </nav>
        </>
      )}
    </PageLayout>
  )
}

export default DocumentsPage
