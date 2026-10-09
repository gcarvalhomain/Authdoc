using System;
using System.Security.Claims;
using Authdoc.Application.DTOs;
using Authdoc.Application.Services;
using Authdoc.Models.Entities;
using Authdoc.Responses;
using Authdoc.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;


namespace Authdoc.Endpoints;

public static class EndpointsUser
{
    private const int MaxPageSize = 100;

    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapGet("/api/users", async (UserService userService, int page = 1, int pageSize = 10) =>
            {
                if (page < 1)
                {
                    return Results.BadRequest(ApiErrors.PageInvalid);
                }

                if (pageSize < 1 || pageSize > MaxPageSize)
                {
                    return Results.BadRequest(ApiErrors.PageSizeInvalid);
                }

                var users = await userService.ListAsync(page, pageSize);

                return Results.Ok(users);
            })
            .RequireAuthorization("Admin");
        app.MapGet("/api/users/{id}", async (Guid id, UserService userService) =>
            {
                var user = await userService.GetByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound(ApiErrors.UserNotFound);
                }

                return Results.Ok(user);
            })
            .RequireAuthorization("Admin");
        app.MapPut("/api/users/{id}", async (Guid id, UpdateUserRequest request, UserService userService) =>
            {
                request.Email = EmailValidator.Normalize(request.Email);
                var validationError = ValidateUpdate(request);
                if (validationError is not null)
                {
                    return Results.BadRequest(validationError);
                }

                var userExist = await userService.EmailBelongsToAnotherUserAsync(id, request.Email);
                if (userExist)
                {
                    return Results.Conflict(ApiErrors.EmailAlreadyInUse);
                }

                var update = await userService.UpdateAsync(id, request);
                if (!update)
                {
                    return Results.NotFound(ApiErrors.UserNotFound);
                }

                var user = await userService.GetByIdAsync(id);

                return Results.Ok(user);
            })
            .RequireAuthorization("Admin");
        app.MapPatch("/api/users/{id}/role", async (Guid id, UpdateUserRoleRequest request, ClaimsPrincipal currentUser, UserService userService) =>
            {
                var role = ParseRole(request.Role);
                if (role is null)
                {
                    return Results.BadRequest(ApiErrors.RoleInvalid);
                }

                var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId == id.ToString())
                {
                    return Results.BadRequest(ApiErrors.CannotChangeOwnRole);
                }

                var result = await userService.ChangeRoleAsync(id, role.Value);
                if (result == ChangeRoleResult.UserNotFound)
                {
                    return Results.NotFound(ApiErrors.UserNotFound);
                }

                if (result == ChangeRoleResult.LastAdmin)
                {
                    return Results.Conflict(ApiErrors.LastAdmin);
                }

                var user = await userService.GetByIdAsync(id);

                return Results.Ok(user);
            })
            .RequireAuthorization("Admin");
        app.MapDelete("/api/users/{id}", async (Guid id, ClaimsPrincipal currentUser, UserService userService) =>
            {
                var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId == id.ToString())
                {
                    return Results.BadRequest(ApiErrors.CannotDeleteOwnUser);
                }

                var result = await userService.DeleteAsync(id);
                if (result == DeleteUserResult.UserNotFound)
                {
                    return Results.NotFound(ApiErrors.UserNotFound);
                }

                if (result == DeleteUserResult.LastAdmin)
                {
                    return Results.Conflict(ApiErrors.LastAdmin);
                }

                return Results.NoContent();
            })
            .RequireAuthorization("Admin");
    }

    private static ErrorResponse? ValidateUpdate(UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return ApiErrors.EmailRequired;
        }

        if (!EmailValidator.IsValid(request.Email))
        {
            return ApiErrors.EmailInvalid;
        }

        if (request.Age < 18)
        {
            return ApiErrors.AgeTooLow;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiErrors.NameRequired;
        }

        return null;
    }

    private static UserRole? ParseRole(string? role)
    {
        foreach (var value in Enum.GetValues<UserRole>())
        {
            if (string.Equals(value.ToString(), role, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }
}