using System;
using Authdoc.Application.DTOs;
using Authdoc.Application.Services;
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
        app.MapDelete("/api/users/{id}", async (Guid id, UserService userService) =>
            {
                var user = await userService.DeleteAsync(id);
                if (!user)
                {
                    return Results.NotFound("User not found");
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
}