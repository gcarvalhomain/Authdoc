import { authRequest } from './session.ts'
import type { DocumentResponse, PagedResponse } from './types.ts'

// One function per call of /api/documents, like auth.ts does for /api/auth.
// Every route here needs a signed-in user, so all of them use authRequest.

// One page of the documents of the signed-in user, the oldest first.
export function listDocuments(
  page: number,
  pageSize: number,
): Promise<PagedResponse<DocumentResponse>> {
  // URLSearchParams builds "page=2&pageSize=10" and escapes the values.
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return authRequest<PagedResponse<DocumentResponse>>(`/documents?${query}`)
}
