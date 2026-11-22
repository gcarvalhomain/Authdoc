import type { DocumentRequest, DocumentResponse } from '../api/types.ts'

// ---------- The values of the document form ----------

// The size limits of the API (the constants in Authdoc/Src/Models/Entities/Document.cs).
// The form uses them so the user cannot type more. The API still checks them:
// the browser limit is only a help, anyone can call the API without this page.
export const documentLimits = {
  type: 50,
  name: 100,
  number: 50,
  description: 500,
}

// What the form fields hold. Every field of a form is text, also the dates
// ("2026-11-22", or "" when the field is empty), so the form keeps strings
// here and turns them into a DocumentRequest only when it saves.
export type DocumentFormValues = {
  type: string
  name: string
  number: string
  description: string
  issuedAt: string
  expiresAt: string
}

export const emptyDocumentValues: DocumentFormValues = {
  type: '',
  name: '',
  number: '',
  description: '',
  issuedAt: '',
  expiresAt: '',
}

// The values of an existing document, to fill in the edit form.
// A date field and the API use the same format ("2026-11-22"), so the dates
// go into the fields as they are, with no time zone problem.
export function toFormValues(document: DocumentResponse): DocumentFormValues {
  return {
    type: document.type,
    name: document.name,
    number: document.number,
    description: document.description ?? '',
    issuedAt: document.issuedAt,
    expiresAt: document.expiresAt ?? '',
  }
}

// The body that the API expects. The API trims the texts itself.
export function toDocumentRequest(values: DocumentFormValues): DocumentRequest {
  return {
    type: values.type,
    name: values.name,
    number: values.number,
    // An empty description is "no description".
    description: values.description.trim() === '' ? null : values.description,
    // An empty issue date is left out (undefined is not written to the JSON),
    // so the API answers ISSUED_AT_REQUIRED.
    issuedAt: values.issuedAt === '' ? undefined : values.issuedAt,
    // An empty expiry date means "it does not expire".
    expiresAt: values.expiresAt === '' ? null : values.expiresAt,
  }
}

// ---------- Moving between the list and the form ----------
//
// React Router can send a hidden "state" with a navigation. It is not in the
// address, and it is kept in the browser history of that page.

// What the list sends to the form: the address of the list page to go back to.
export type DocumentFormState = { returnTo: string }

// What the form sends back to the list: the success message to show.
export type DocumentsListState = { message: string }

// Where the form goes after Save or Cancel. Only an address of the list is
// accepted, so a strange state cannot send the user to another place.
export function readReturnTo(state: unknown): string {
  const returnTo = (state as Partial<DocumentFormState> | null)?.returnTo
  return typeof returnTo === 'string' && returnTo.startsWith('/documents') ? returnTo : '/documents'
}

export function readMessage(state: unknown): string | null {
  const message = (state as Partial<DocumentsListState> | null)?.message
  return typeof message === 'string' ? message : null
}
