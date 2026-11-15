import { ApiError } from './client.ts'

const sessionEnded = 'Your session has ended. Please sign in again.'
const somethingWentWrong = 'Something went wrong. Please try again.'

// Texts for the error codes where the API message is too technical for a user.
// Every other code shows the message that the API sent, for example
// "Email is already in use".
const messages: Record<string, string> = {
  UNAUTHORIZED: sessionEnded,
  INVALID_REFRESH_TOKEN: sessionEnded,
  TOO_MANY_REQUESTS: 'Too many attempts. Please wait a moment and try again.',
  INTERNAL_ERROR: somethingWentWrong,
  NETWORK_ERROR: 'Could not reach the server. Check your connection and try again.',
  UNKNOWN_ERROR: somethingWentWrong,
}

// Returns the text to show to the user for an error that a page caught.
export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return messages[error.code] ?? error.message
  }

  // Not an error from apiRequest, so it is a mistake in the frontend code.
  return somethingWentWrong
}
