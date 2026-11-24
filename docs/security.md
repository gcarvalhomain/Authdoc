# Security

The main promise of Authdoc:

> **Each user accesses only their own documents.**

The `Admin` role manages accounts, but it gives no access to other users' documents.

## How the API protects users and documents

### Authentication

- **JWT, signed with HMAC-SHA256.** The token carries the user id, name, email, role and security stamp. The API checks the issuer, audience, lifetime and signature on every request.
- **A database check on every request.** After the signature, the API checks that the user still exists, still has the role in the token and still has the same security stamp. So a deleted user, a changed role or a changed password blocks the old token at once (`401 UNAUTHORIZED`).
- **Security stamp.** A password change creates a new stamp, so every access token made before it stops working, on every device.
- **Refresh token with rotation.** Each use returns a new refresh token and revokes the old one. Only its SHA-256 hash is in the database. `RevokedAt` is a concurrency token, so when two requests use the same refresh token at the same time, only one gets a new session.

### Passwords

- Hashed with PBKDF2 and a random salt (`IPasswordHasher<T>` from ASP.NET Core Identity). The hash format has a version, so the algorithm can change later.
- A password longer than 128 characters is rejected before hashing, so nobody can make the server hash huge texts.
- A sign-in with an unknown email still hashes the password, so the answer takes about the same time and does not tell whether the email exists.

### Authorization

- **Roles (RBAC).** An `Admin` policy protects the user management routes.
- **Owner filter.** Every document query filters by the user id from the token (`ClaimTypes.NameIdentifier`), never from the body or the address. Another user's document answers `404`, so its existence is never revealed.
- **GUID ids.** Ids are not sequential, so nobody can guess the next one.
- **Role changes.** Only `PATCH /api/users/{id}/role` changes a role. The last admin check runs in a transaction that locks the admin rows (`UPDLOCK, HOLDLOCK`), so two requests at the same time cannot remove the last two admins.

### Other protections

- **Rate limiting** on sign-up, sign-in, password change, refresh and logout (see [API reference](api.md#limits)).
- **Secrets outside the repository:** User Secrets in development, environment variables elsewhere (see [Development](development.md#configuration)).
- **No internal details in errors:** an unexpected exception is logged in one place and answers `500 INTERNAL_ERROR`.
- **Frontend:** the access token lives only in memory, and the page loads no fonts, scripts or styles from other servers.

## Known limitations

| Limitation | Impact | Possible solution |
|---|---|---|
| A logout does not end the current JWT | The access token of that device keeps working for up to 60 minutes (its refresh token is already revoked) | A shorter token lifetime, or a list of revoked token ids |
| A reused refresh token is only rejected | An old refresh token that comes back gets `401`, but the other sessions of that user stay active | Revoke all sessions of the user when an old token comes back (reuse detection) |
| The refresh token is in `localStorage` | An XSS bug could read it | An `httpOnly` cookie set by the API |
| Rate limiting is in memory and per IP | It resets on restart, is not shared between instances, and behind a proxy everyone can look like the same IP | `ForwardedHeaders` setup and a shared store (for example Redis) |
| The email is not verified | Anyone can sign up with an email that is not theirs | A confirmation link before the first sign-in |
| Sign-up tells whether an email is used | `409 EMAIL_ALREADY_IN_USE` lets anyone test emails | The same answer for every sign-up, and the result by email |
| One database query per authenticated request | A small extra query (by primary key) on every request | A short cache if the load grows |
| Tests use an in-memory database | The unique email index and the locks are not tested by a real database | Tests with a real SQL Server (for example Testcontainers) |

## Personal data

Authdoc is a portfolio project with no public environment, and it must not receive real documents from other people. Real use would need encryption at rest, a retention policy and deletion on request, as the GDPR (LGPD in Brazil) asks for personal data.
