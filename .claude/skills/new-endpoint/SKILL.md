---
name: new-endpoint
description: Adds a new feature or Minimal API endpoint to Authdoc, from the entity to the README. Use when the task is to create a new endpoint, resource or feature in the API.
---

# New endpoint in Authdoc

## Required order (do not skip steps)
1. Entity
2. Migration (clear name, e.g. AddDocumentsTable). Read the generated file and warn if there is any data loss.
3. Input and output DTOs
4. Service with the business logic
5. Endpoint (Minimal API, never a Controller) calling only the service
6. New error codes in ApiErrors.cs
7. Tests: follow the write-tests skill
8. Update the README tables

## Layer rules
- Endpoint → Service → DbContext. The endpoint never touches the DbContext.
- Never return an entity directly, always a DTO
- Owned resources: OwnerId comes from the token claim, never from the request body, and every query filters by owner

## When finished
- `dotnet build` and `dotnet test` green
- List the files created or changed
