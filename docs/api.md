# API reference

Base address in development: `http://localhost:5297`. In `Development`, the interactive documentation (Scalar) is at `http://localhost:5297/scalar`.

Every route starts with `/api`. Routes marked **Authenticated** need the header `Authorization: Bearer <token>`.

## First complete flow

```http
### 1. Create an account (no token needed)
POST http://localhost:5297/api/auth/register
Content-Type: application/json

{
  "name": "Maria Silva",
  "email": "maria@example.com",
  "password": "password123",
  "confirmationPassword": "password123"
}

### 2. Sign in with the new user
POST http://localhost:5297/api/auth/login
Content-Type: application/json

{ "email": "maria@example.com", "password": "password123" }

### 3. Add a document (use the token returned in step 2)
POST http://localhost:5297/api/documents
Authorization: Bearer <token>
Content-Type: application/json

{
  "type": "Driver's license",
  "name": "My driver's license",
  "number": "AB123456",
  "issuedAt": "2024-01-10",
  "expiresAt": "2034-01-10"
}
```

The initial admin signs in the same way, with `admin@system.com` and the password from `SeedData:AdminPassword` (see [Development](development.md#the-first-admin)).

## Endpoints

### Auth

| Method | Route | Access | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Creates an account, always with the `User` role |
| `POST` | `/api/auth/login` | Public | Returns the JWT, the refresh token, their expiry times and the user |
| `POST` | `/api/auth/refresh` | Public | Exchanges a valid refresh token for a new pair of tokens |
| `POST` | `/api/auth/logout` | Public | Revokes the refresh token in the body (`{ "refreshToken": "..." }`) and returns `204` |
| `GET` | `/api/auth/me` | Authenticated | Returns the current user, read from the database |
| `PUT` | `/api/auth/password` | Authenticated | Changes the user's own password and ends every session of the user |

### Users

| Method | Route | Access | Description |
|---|---|---|---|
| `GET` | `/api/users?page=1&pageSize=10` | Admin | Lists users with pagination (`pageSize` from 1 to 100) |
| `GET` | `/api/users/{id}` | Admin | Returns one user |
| `PUT` | `/api/users/{id}` | Admin | Updates name and email |
| `PATCH` | `/api/users/{id}/role` | Admin | Changes the role: `{ "role": "Admin" }` or `{ "role": "User" }` |
| `DELETE` | `/api/users/{id}` | Admin | Deletes a user and their documents |

### Documents

Every route works only on the documents of the user in the token. The `Admin` role gives no extra access here.

| Method | Route | Access | Description |
|---|---|---|---|
| `POST` | `/api/documents` | Authenticated | Adds a document and returns `201` with `Location` |
| `GET` | `/api/documents?page=1&pageSize=10` | Authenticated | Lists the user's documents, oldest first (`pageSize` from 1 to 100) |
| `GET` | `/api/documents/{id}` | Authenticated | Returns one document |
| `PUT` | `/api/documents/{id}` | Authenticated | Replaces all the fields of a document |
| `DELETE` | `/api/documents/{id}` | Authenticated | Deletes a document and returns `204` |

Body of `POST` and `PUT`:

```json
{
  "type": "Driver's license",
  "name": "My driver's license",
  "number": "AB123456",
  "description": "Category B",
  "issuedAt": "2024-01-10",
  "expiresAt": "2034-01-10"
}
```

## Business rules

### Accounts

- The email must be valid (any domain) and unique. It is trimmed and lowercased before it is validated, searched or saved.
- The name is required, with at most 100 characters, and it is trimmed.
- The password has 8 to 128 characters, cannot be only spaces, and is confirmed on sign-up and on a password change.
- A password longer than 128 characters is never hashed. It gets the same answer as a wrong password (`401 INVALID_CREDENTIALS` on sign-in, `400 CURRENT_PASSWORD_INCORRECT` on a password change).
- Sign-up always creates a `User`. A role sent in the body is ignored, and the update route cannot change the role.
- Only `PATCH /api/users/{id}/role` changes a role. An admin cannot change their own role or delete their own user, and the last admin cannot be demoted or deleted.

### Sessions

- The JWT lives for 60 minutes. The refresh token lives for 7 days, works only once (each use returns a new one) and is stored only as a SHA-256 hash.
- Logout revokes only the refresh token that was sent; the other sessions stay active. It always answers `204`, so it does not tell whether a token exists.
- A password change revokes every refresh token of the user and makes every older JWT stop working.
- A used refresh token that comes back is treated as stolen: every session of the user ends (all refresh tokens and all JWTs), and the answer is `401 INVALID_REFRESH_TOKEN`.
- A role change or a user deletion works at once: the next request with an old token gets `401 UNAUTHORIZED`.

### Documents

- A document belongs to the user in the token. An owner id in the body is ignored.
- Another user's document answers `404`, the same as a document that does not exist.
- Type and number are required (at most 50 characters each), the name is required (at most 100), and the description is optional (at most 500). Text fields are trimmed, and a blank description is saved as `null`.
- The issue date is required and cannot be in the future (one day of tolerance covers clients ahead of UTC).
- The expiry date is optional. When it is sent, it must be after the issue date. An expired document can still be added.

### Limits

- Sign-up, sign-in and password change: 5 requests per minute per IP. Refresh and logout: 30. Above that, the answer is `429`.

## Error format

Every error answer has the same shape, with a stable `code` for the client and a readable `message`:

```json
{ "code": "EMAIL_INVALID", "message": "Email is invalid" }
```

| Status | Codes |
|---|---|
| `400` | `INVALID_REQUEST`, `NAME_REQUIRED`, `NAME_TOO_LONG`, `EMAIL_REQUIRED`, `EMAIL_INVALID`, `PASSWORD_REQUIRED`, `PASSWORD_TOO_SHORT`, `PASSWORD_TOO_LONG`, `PASSWORDS_DO_NOT_MATCH`, `ROLE_INVALID`, `CANNOT_CHANGE_OWN_ROLE`, `CANNOT_DELETE_OWN_USER`, `CURRENT_PASSWORD_REQUIRED`, `CURRENT_PASSWORD_INCORRECT`, `NEW_PASSWORD_SAME_AS_CURRENT`, `REFRESH_TOKEN_REQUIRED`, `PAGE_INVALID`, `PAGE_SIZE_INVALID`, `TYPE_REQUIRED`, `TYPE_TOO_LONG`, `NUMBER_REQUIRED`, `NUMBER_TOO_LONG`, `DESCRIPTION_TOO_LONG`, `ISSUED_AT_REQUIRED`, `ISSUED_AT_IN_FUTURE`, `EXPIRES_AT_BEFORE_ISSUED_AT` |
| `401` | `INVALID_CREDENTIALS`, `INVALID_REFRESH_TOKEN`, `UNAUTHORIZED` |
| `403` | `FORBIDDEN` |
| `404` | `USER_NOT_FOUND`, `DOCUMENT_NOT_FOUND` |
| `409` | `EMAIL_ALREADY_IN_USE`, `LAST_ADMIN` |
| `429` | `TOO_MANY_REQUESTS` |
| `500` | `INTERNAL_ERROR` |

The codes live in `Authdoc/Src/Responses/ApiErrors.cs`. An unexpected exception is logged in one place (`GlobalExceptionHandler`) and answers `500 INTERNAL_ERROR`, without internal details.
