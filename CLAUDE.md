# Authdoc: project rules

## Project
- ASP.NET Core 10 Minimal API, EF Core 10 + SQL Server, JWT, xUnit.
- Identity is implemented. The documents module is the next step.
- Main promise: each user accesses only their own documents.

## How to work with Admin
- The admin writes the documents module code. Give theory, questions
  and small examples. Write finished code only when the admin asks for it.
- Do not change files unless the admin asks.

## Commands
- Build: `dotnet build`
- Test: `dotnet test`
- Format: `dotnet format`
- Run: `dotnet run --project Authdoc`
- Migration: `dotnet ef migrations add <Name> --project Authdoc`

## Architecture
- Flow: Endpoint -> Service -> DbContext. No layer is skipped.
- Endpoints do HTTP only: normalize input, validate, call the service,
  map the result to a status code.
- Validation is a private static `ValidateX` method that returns `ErrorResponse?`.
- Services hold business rules and database access. They do not return
  `Results` and do not read `HttpContext`.
- Expected failures return a result enum (like `DeleteUserResult`) or `null`.
  Exceptions are only for unexpected errors (`GlobalExceptionHandler`).
- Never return an entity. Response DTOs are `sealed record` with a
  static `From(entity)` method. Request DTOs are classes.
- Every error body is an `ErrorResponse` from `ApiErrors.cs`, with an
  UPPER_SNAKE_CASE code, grouped by status code.
- Size limits are constants on the entity (like `User.NameMaxLength`),
  used by both the validation and the `DbContext` configuration.
- Ids are `Guid`. Dates are `DateTime.UtcNow`. Entities have `CreatedAt`/`UpdatedAt`.
- New feature files: `Endpoints{Feature}.cs` with `Map{Feature}Endpoints`,
  `{Feature}Service` registered as Scoped in `Program.cs`.
- Routes are `/api/{resource}`. Use `201` + `Location`, `204` and `409` correctly.
- Lists use `PagedResponse<T>` with `page` / `pageSize` (1 to 100) and a stable order.

## Security
- The current user id comes from `ClaimTypes.NameIdentifier`, never from
  the body or the route.
- Every document query filters by `OwnerId`. A document of another user
  returns `404`, not `403`.
- Secrets never go in the repo or in `appsettings.json`
  (User Secrets in development, environment variables elsewhere).
- Never log or return passwords, hashes or tokens.
- A role changes only through `PATCH /api/users/{id}/role`.

## Database
- The schema changes only through EF migrations, with a clear PascalCase name.
- Never edit a migration that was already applied. Create a new one.
- Read the generated migration before committing and check for data loss.
- A unique rule needs a database index and a `catch` with `IsUniqueViolation()`.

## Tests
- Every behavior change comes with tests.
- Name: `Method_Condition_ExpectedResult`.
- Body: `// Arrange`, `// Act`, `// Assert`.
- Integration: `IClassFixture<AuthdocApiFactory>` + helpers in `ApiTestHelpers.cs`.
  Assert the status code and the error `code`.
- Unit: service rules with an InMemory context (`CreateContext()`).
- Use a different email in each test (the database is shared inside a test class).
- Run `dotnet test` locally before a merge.

## Git
- Never commit on `main`. Branch first: `feat/`, `fix/`, `refactor/`, `chore/`, `docs/`.
- One commit per concern. Message: `type: short text` in English.
- No AI attribution lines in commits or PRs.
- PRs go to Azure DevOps (`origin`).
- Do not change `deployEnabled` or the pipeline without asking.

## Documentation
- README is in Portuguese. Code, comments, commits and error messages are in English.
- When endpoints, error codes, settings or business rules change,
  update the README in the same PR.
