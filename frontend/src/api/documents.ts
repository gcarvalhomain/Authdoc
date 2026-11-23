import { authRequest } from './session.ts'
import type { DocumentRequest, DocumentResponse, PagedResponse } from './types.ts'

// One function per call of /api/documents, like auth.ts does for /api/auth.
// Every route here needs a signed-in user, so all of them use authRequest.

// How many documents the list page shows at a time.
export const documentsPageSize = 10

// One page of the documents of the signed-in user, the oldest first.
export function listDocuments(
  page: number,
  pageSize: number,
): Promise<PagedResponse<DocumentResponse>> {
  // URLSearchParams builds "page=2&pageSize=10" and escapes the values.
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return authRequest<PagedResponse<DocumentResponse>>(`/documents?${query}`)
}

// One document. The API answers 404 DOCUMENT_NOT_FOUND for a document
// of another user, so nobody can find out that it exists.
export function getDocument(id: string): Promise<DocumentResponse> {
  // encodeURIComponent: the id comes from the address, so it is escaped
  // before it goes into the path.
  return authRequest<DocumentResponse>(`/documents/${encodeURIComponent(id)}`)
}

// Saves a new document and returns it (201 Created).
export function createDocument(request: DocumentRequest): Promise<DocumentResponse> {
  return authRequest<DocumentResponse>('/documents', { method: 'POST', body: request })
}

// Deletes a document. The API answers 204 No Content, so there is nothing to return.
export function deleteDocument(id: string): Promise<void> {
  return authRequest<void>(`/documents/${encodeURIComponent(id)}`, { method: 'DELETE' })
}

// Replaces all the fields of a document and returns it.
export function updateDocument(id: string, request: DocumentRequest): Promise<DocumentResponse> {
  return authRequest<DocumentResponse>(`/documents/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: request,
  })
}
