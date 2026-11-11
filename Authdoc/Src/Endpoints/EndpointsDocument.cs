using System;
using System.Security.Claims;
using Authdoc.Application.DTOs;
using Authdoc.Application.Services;
using Authdoc.Models.Entities;
using Authdoc.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Authdoc.Endpoints;

public static class EndpointsDocument
{
    private const int MaxPageSize = 100;

    public static void MapDocumentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/documents", async (CreateDocumentRequest request, ClaimsPrincipal currentUser, DocumentService documentService) =>
            {
                request.Type = NormalizeText(request.Type);
                request.Name = NormalizeText(request.Name);
                request.Number = NormalizeText(request.Number);
                request.Description = NormalizeOptionalText(request.Description);

                var validationError = Validate(request.Type, request.Name, request.Number, request.Description, request.IssuedAt, request.ExpiresAt);
                if (validationError is not null)
                {
                    return Results.BadRequest(validationError);
                }

                if (!TryGetUserId(currentUser, out var ownerId))
                {
                    return Results.Json(ApiErrors.Unauthorized, statusCode: StatusCodes.Status401Unauthorized);
                }

                var document = await documentService.CreateAsync(ownerId, request);

                return Results.Created($"/api/documents/{document.Id}", document);
            })
            .RequireAuthorization();
        app.MapGet("/api/documents/{id}", async (Guid id, ClaimsPrincipal currentUser, DocumentService documentService) =>
            {
                if (!TryGetUserId(currentUser, out var ownerId))
                {
                    return Results.Json(ApiErrors.Unauthorized, statusCode: StatusCodes.Status401Unauthorized);
                }

                var document = await documentService.GetByIdAsync(ownerId, id);
                if (document is null)
                {
                    return Results.NotFound(ApiErrors.DocumentNotFound);
                }

                return Results.Ok(document);
            })
            .RequireAuthorization();
        app.MapGet("/api/documents", async (ClaimsPrincipal currentUser, DocumentService documentService, int page = 1, int pageSize = 10) =>
            {
                if (page < 1)
                {
                    return Results.BadRequest(ApiErrors.PageInvalid);
                }

                if (pageSize < 1 || pageSize > MaxPageSize)
                {
                    return Results.BadRequest(ApiErrors.PageSizeInvalid);
                }

                if (!TryGetUserId(currentUser, out var ownerId))
                {
                    return Results.Json(ApiErrors.Unauthorized, statusCode: StatusCodes.Status401Unauthorized);
                }

                var documents = await documentService.ListAsync(ownerId, page, pageSize);

                return Results.Ok(documents);
            })
            .RequireAuthorization();
        app.MapPut("/api/documents/{id}", async (Guid id, UpdateDocumentRequest request, ClaimsPrincipal currentUser, DocumentService documentService) =>
            {
                request.Type = NormalizeText(request.Type);
                request.Name = NormalizeText(request.Name);
                request.Number = NormalizeText(request.Number);
                request.Description = NormalizeOptionalText(request.Description);

                var validationError = Validate(request.Type, request.Name, request.Number, request.Description, request.IssuedAt, request.ExpiresAt);
                if (validationError is not null)
                {
                    return Results.BadRequest(validationError);
                }

                if (!TryGetUserId(currentUser, out var ownerId))
                {
                    return Results.Json(ApiErrors.Unauthorized, statusCode: StatusCodes.Status401Unauthorized);
                }

                var document = await documentService.UpdateAsync(ownerId, id, request);
                if (document is null)
                {
                    return Results.NotFound(ApiErrors.DocumentNotFound);
                }

                return Results.Ok(document);
            })
            .RequireAuthorization();
        app.MapDelete("/api/documents/{id}", async (Guid id, ClaimsPrincipal currentUser, DocumentService documentService) =>
            {
                if (!TryGetUserId(currentUser, out var ownerId))
                {
                    return Results.Json(ApiErrors.Unauthorized, statusCode: StatusCodes.Status401Unauthorized);
                }

                var deleted = await documentService.DeleteAsync(ownerId, id);
                if (!deleted)
                {
                    return Results.NotFound(ApiErrors.DocumentNotFound);
                }

                return Results.NoContent();
            })
            .RequireAuthorization();
    }

    private static bool TryGetUserId(ClaimsPrincipal currentUser, out Guid userId)
    {
        return Guid.TryParse(currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
    }

    private static string NormalizeText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ErrorResponse? Validate(string type, string name, string number, string? description, DateOnly issuedAt, DateOnly? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return ApiErrors.TypeRequired;
        }

        if (type.Length > Document.TypeMaxLength)
        {
            return ApiErrors.TypeTooLong;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ApiErrors.NameRequired;
        }

        if (name.Length > Document.NameMaxLength)
        {
            return ApiErrors.NameTooLong;
        }

        if (string.IsNullOrWhiteSpace(number))
        {
            return ApiErrors.NumberRequired;
        }

        if (number.Length > Document.NumberMaxLength)
        {
            return ApiErrors.NumberTooLong;
        }

        if (description is not null && description.Length > Document.DescriptionMaxLength)
        {
            return ApiErrors.DescriptionTooLong;
        }

        if (issuedAt == default)
        {
            return ApiErrors.IssuedAtRequired;
        }

        // The client can be up to one day ahead of UTC, so tomorrow (UTC) is still accepted.
        var latestIssueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        if (issuedAt > latestIssueDate)
        {
            return ApiErrors.IssuedAtInFuture;
        }

        if (expiresAt is not null && expiresAt <= issuedAt)
        {
            return ApiErrors.ExpiresAtBeforeIssuedAt;
        }

        return null;
    }
}
