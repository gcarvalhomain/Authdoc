---
name: write-tests
description: Writes, reviews or fixes xUnit tests in Authdoc, both unit and integration. Use whenever the task involves creating tests for an endpoint, service or rule, or fixing broken tests.
---

# Tests in Authdoc

## Required pattern
- Name: Method_Condition_ExpectedResult
  e.g. UpdateUser_WithTooLongName_Returns400NameTooLong
- // Arrange, // Act, // Assert comments in every test
- Integration tests use AuthdocApiFactory and the helpers in ApiTestHelpers.cs
- Every endpoint test asserts the status code AND the error code (from ApiErrors.cs)
- Tests live in Authdoc.Tests/ (unit tests in the root, integration tests in Authdoc.Tests/Integration/)

## Steps
1. Read the target class or endpoint
2. List the scenarios before writing: success, validation error, not found, no permission
3. For owned resources: include the "another user gets 404" test
4. Write the tests reusing ApiTestHelpers.cs (do not duplicate helpers)
5. Run `dotnet test` and only finish when everything is green

## Don't
- Don't change production code to make a test pass. If a test reveals a bug, stop and report it.
- Don't use a real database outside AuthdocApiFactory
