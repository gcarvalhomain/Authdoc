# Frontend

The web app is in `frontend/`: React 19, TypeScript, Vite and React Router, with plain CSS and no UI library.

## Running

Start the API first (see [Development](development.md#running)), then:

```bash
cd frontend
npm install
npm run dev
```

The app opens at `http://localhost:5173`.

| Command | What it does |
|---|---|
| `npm run dev` | Development server with hot reload |
| `npm run build` | Checks the types and builds the production files into `frontend/dist/` |
| `npm run lint` | Runs the linter (oxlint) |
| `npm run preview` | Serves the production build locally |

## Pages

The routes are in `frontend/src/App.tsx`.

| Route | Access | Page |
|---|---|---|
| `/` | Public | Home, with links to sign in or create an account (or to the documents and the account, when signed in) |
| `/login` | Public | Sign in. After it, the page the user tried to open, or `/account` |
| `/register` | Public | Create an account. After it, the sign-in page opens with the email filled in |
| `/account` | Signed in | My account: name, email, role and sign-up date, and a "Change password" link |
| `/account/password` | Signed in | Change password. After it, the sign-in page opens with "Your password was changed" |
| `/documents` | Signed in | My documents: 10 per page, with Edit and Delete in each row |
| `/documents/new` | Signed in | Add a document |
| `/documents/:id/edit` | Signed in | Edit a document |
| any other | Public | Page not found |

### My documents

- The page number is in the address (`/documents?page=2`), so a reload or the Back button keeps the page. A wrong value opens page 1.
- The page shows a loading text, a message when there are no documents, and the error with a "Try again" button when the call fails.
- Dates without a time (`issuedAt`, `expiresAt`) are shown in UTC. Read as local time, `"2026-11-01"` would show as Oct 31 in Brazil (UTC-3).
- Delete asks inside the row ("Delete this document?") first. After it, the list loads again with "Document deleted."; when the last document of a page is deleted, the page before opens.

### Add and edit

- Both pages use one form (`frontend/src/pages/DocumentForm.tsx`). The Type field suggests common types and accepts any other text.
- Each validation error of the API shows under its field. The fields also stop at the API size limits; the API still checks them.
- After a save, the list opens with "Document added." or "Document updated.".
- A document that does not exist or belongs to another user shows "This document does not exist or was deleted.".

## How it talks to the API

- The API has no CORS setup, so the browser never calls it directly. In development, Vite forwards every `/api` request to `http://localhost:5297` (`frontend/vite.config.ts`). A production server needs the same forward, and must answer every route with `index.html`.
- Every call goes through `apiRequest` (`frontend/src/api/client.ts`). A failure becomes an `ApiError` with the HTTP status and the API `code` (see [Error format](api.md#error-format)). The client adds two codes of its own: `NETWORK_ERROR` (the API could not be reached) and `UNKNOWN_ERROR` (an answer that is not in the API format).
- `errorMessages.ts` turns a code into the text for the user, and `formErrors.ts` decides which field of a form shows it. On the sign-in page, `INVALID_CREDENTIALS` always shows at the top, so the page does not tell whether the email or the password is wrong.

## Session

The session code is in `frontend/src/api/session.ts` (the tokens) and `frontend/src/auth/` (the React side).

| Part | Where it lives | Why |
|---|---|---|
| Access token (JWT) | Only in memory | It lives for one hour, so losing it on a reload is fine |
| Refresh token | `localStorage` | After a reload, the app exchanges it for new tokens, so the user stays signed in |
| Signed-in user | `AuthContext` (React Context) | Every page reads it with `useAuth()` |

- **Automatic refresh:** when a call gets `401 UNAUTHORIZED`, the app refreshes the tokens and sends the call once more. Only one refresh runs at a time, because the API accepts each refresh token only once.
- **Session ended:** when the API rejects the refresh token, the app forgets the tokens and opens the sign-in page with "Your session has ended".
- **Sign out:** the app forgets the tokens at once and calls `POST /api/auth/logout`. The other devices stay signed in.
- **Password change:** the API ends every session of the user, so the app signs out and asks for a new sign-in.
- **Protected pages:** `RequireAuth` sends a user without a session to the sign-in page, and back to the same page after the sign-in.
- **Known limitation:** any script on the page can read `localStorage`, so an XSS bug could steal the refresh token. Two tabs that refresh at the same moment can also sign one of them out. See [Security](security.md#known-limitations).

## Structure

```
frontend/
├── index.html
├── vite.config.ts      # Dev server and /api forward
└── src/
    ├── main.tsx        # Entry point, with the router and the session provider
    ├── App.tsx         # Routes
    ├── index.css       # Visual identity: colors, fonts, sizes and spacing as CSS variables
    ├── api/            # API client, session tokens, API calls, types and error messages
    ├── auth/           # Session in React: AuthContext, AuthProvider, RequireAuth
    ├── pages/          # One component per page, plus the document form
    └── shared/         # Shared components: Alert, Button, Input, TextArea, PageLayout
```

The visual identity uses only system fonts, so the page loads nothing from the internet.
