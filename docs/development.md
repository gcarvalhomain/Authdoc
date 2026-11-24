# Development

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server. The default connection string points to `localhost\SQLEXPRESS` with Windows authentication.
- [Node.js](https://nodejs.org) 20.19 or newer, for the frontend
- Optional: `dotnet tool install --global dotnet-ef`, to create migrations

## Configuration

The settings that are not secret are in `Authdoc/appsettings.json`:

| Setting | Default | Meaning |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `localhost\SQLEXPRESS`, database `Authdoc` | The SQL Server database |
| `Jwt:Issuer` / `Jwt:Audience` | `Authdoc` / `Users` | Checked in every token |
| `Jwt:ExpirationInMinutes` | `60` | Lifetime of the access token |
| `Jwt:RefreshTokenExpirationInDays` | `7` | Lifetime of the refresh token |
| `Jwt:RefreshTokenReuseGraceSeconds` | `10` | A used refresh token that comes back within this time only gets `401`, without the reuse alarm |
| `RateLimiting:LoginPermitLimit` | `5` | Sign-in and password change, per minute per IP |
| `RateLimiting:RegisterPermitLimit` | `5` | Sign-up, per minute per IP |
| `RateLimiting:RefreshPermitLimit` | `30` | Refresh and logout, per minute per IP |
| `FORWARDEDHEADERS_ENABLED` | `true` (`false` in `appsettings.Development.json`) | Reads the client IP from the `X-Forwarded-For` header of the Azure proxy (see below) |

### Client IP behind the Azure proxy

On Azure App Service, every request passes through Azure's front-end proxy, which adds the real client IP to the `X-Forwarded-For` header. Without help, the API would see the proxy, and the rate limit would count every client together.

`FORWARDEDHEADERS_ENABLED` is a built-in ASP.NET Core switch (the environment variable `ASPNETCORE_FORWARDEDHEADERS_ENABLED`). When it is `true`, the framework adds the Forwarded Headers middleware first in the pipeline. It trusts the proxy in front of the API and reads only the last value of `X-Forwarded-For`, which is the one the proxy adds, so a fake value sent by a client is ignored.

It is `false` in development: there is no proxy on your machine, so a client could send a new fake IP on every request and never reach the rate limit. Keep it `false` anywhere the API can be called without a proxy in front of it.

The secrets are never in the repository. In development they come from [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets); in any other environment, from environment variables:

| Setting | Environment variable | Required |
|---|---|---|
| JWT signing key | `Jwt__Key` | Always |
| Initial admin password | `SeedData__AdminPassword` | While no admin exists |
| Connection string | `ConnectionStrings__DefaultConnection` | Outside your machine |

### JWT signing key

`Jwt:Key` must have at least 32 bytes (HS256); with less, the API does not start. Generate a random key once:

```powershell
cd Authdoc
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($bytes))
```

```bash
cd Authdoc
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64 | tr -d '\n')"
```

### Initial admin password

```bash
cd Authdoc
dotnet user-secrets set "SeedData:AdminPassword" "<your-strong-password>"
```

## The first admin

Anyone can create an account, but a new account is always a `User`, and only an admin can give the `Admin` role. So the first admin is created when the API starts (`Authdoc/Src/Data/DbInitializer.cs`):

1. It applies the pending migrations. The database is created if it does not exist.
2. If an admin already exists, it does nothing, so it is safe on every start.
3. If a normal user already has the admin email, the API stops with a clear message. It never promotes that user, because that person does not know the seed password. This can only happen when no admin exists anymore, for example after a database restore. To fix it, delete or rename that user and start again.
4. Otherwise it creates the admin `admin@system.com` with the password from `SeedData:AdminPassword`.

Change the admin password on the first sign-in in any shared environment.

## Running

```bash
dotnet run --project Authdoc      # API on http://localhost:5297
cd frontend && npm install && npm run dev   # app on http://localhost:5173
```

The frontend forwards every `/api` request to the API, so start the API first. See [Frontend](frontend.md) for the other npm commands.

## Database

The schema changes only through EF Core migrations, and the API applies them at startup.

```bash
dotnet ef migrations add <PascalCaseName> --project Authdoc --output-dir Src/Data/Migrations
```

Read the generated migration before the commit, and never edit a migration that was already applied.

## Tests

```bash
dotnet test
```

| Type | What it covers |
|---|---|
| Unit | Email validation, the admin seed, and the rules of `AuthService` (too long passwords, logout, a refresh token used twice at the same time, reuse detection), `UserService` (last admin, role change, deletion) and `DocumentService` (each operation reaches only the owner's documents) |
| Integration | The whole API over HTTP with `WebApplicationFactory`: sign-up, sign-in, refresh, logout, password change, users, documents (including `404` for another user's document), pagination, rate limiting and error codes |

The tests use an in-memory database and their own settings, so they need neither SQL Server nor User Secrets. The frontend has no automated tests yet; `npm run lint` and `npm run build` check it.

## CI/CD

The pipeline (`azure-pipelines.yml`) runs on every change to `main`:

1. Restores and builds in `Release`
2. Runs the tests; a failed test stops the pipeline
3. Publishes the API as the `drop` artifact
4. Deploys to Azure App Service, only when `deployEnabled` is `'true'`

The deploy is turned off until the production database and settings exist. The pipeline does not build the frontend yet.
