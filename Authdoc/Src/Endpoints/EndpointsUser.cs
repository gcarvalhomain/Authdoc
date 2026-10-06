using System;
using System.Security.Claims;
using Authdoc.Application.DTOs;
using Authdoc.Application.Services;
using Authdoc.Models.Entities;
using Authdoc.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;


namespace Authdoc.Endpoints;

public static class EndpointsUser
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapGet("/api/users/{id}", async (Guid id, UserService userService) =>
            {
                var user = await userService.GetByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound("User not found");
                }

                return Results.Ok(user);
            })
            .RequireAuthorization("Admin");
        app.MapPut("/api/users/{id}", async (Guid id, UpdateUserRequest request, UserService userService) =>
            {
                var validationError = ValidateUpdate(request);
                if (validationError is not null)
                {
                    return Results.BadRequest(validationError);
                }

                var userExist = await userService.EmailBelongsToAnotherUserAsync(id, request.Email);
                if (userExist)
                {
                    return Results.Conflict("Email is already in use");
                }

                var update = await userService.UpdateAsync(id, request);
                if (!update)
                {
                    return Results.NotFound("User not found");
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
                    return Results.BadRequest("Role must be Admin or User");
                }

                var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId == id.ToString())
                {
                    return Results.BadRequest("You cannot change your own role");
                }

                var result = await userService.ChangeRoleAsync(id, role.Value);
                if (result == ChangeRoleResult.UserNotFound)
                {
                    return Results.NotFound("User not found");
                }

                if (result == ChangeRoleResult.LastAdmin)
                {
                    return Results.Conflict("Cannot remove the last admin");
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
                    return Results.BadRequest("You cannot delete your own user");
                }

                var result = await userService.DeleteAsync(id);
                if (result == DeleteUserResult.UserNotFound)
                {
                    return Results.NotFound("User not found");
                }

                if (result == DeleteUserResult.LastAdmin)
                {
                    return Results.Conflict("Cannot remove the last admin");
                }

                return Results.NoContent();
            })
            .RequireAuthorization("Admin");
    }

    private static string? ValidateUpdate(UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email is required";
        }

        if (!EmailValidator.IsValid(request.Email))
        {
            return "Email is invalid";
        }

        if (request.Age < 18)
        {
            return "Age must be 18 years of age or older";
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required";
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