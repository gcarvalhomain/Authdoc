using Authdoc.Application.DTOs;
using Authdoc.Responses;
using Authdoc.Application.Services;


namespace Authdoc.Endpoints;

public static class EndpointsUser
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapGet("Api/Users/{id}", async (int id, UserService userService) =>
        {
            var user = await userService.GetByIdAsync(id);
            if (user is null)
            {
                return Results.NotFound(new ErrorResponse
                {
                    Message = "User not found",
                });
            }

            return Results.Ok(user);
        });
        app.MapPut("Api/Users/{id}", async (int id, UpdateUserRequest request, UserService userService) =>
            {
                var validationError = ValidateUpdate(request);
                if (validationError is not null)
                {
                    return Results.BadRequest(new ErrorResponse
                    {
                        Message = validationError
                    });
                }

                var userExist = await userService.EmailBelongsToAnotherUserAsync(id, request.Email);
                if (userExist)
                {
                    return Results.Conflict(new ErrorResponse
                    {
                        Message = "User already exists"
                    });
                }

                var update = await userService.UpdateAsync(id, request);
                if (!update)
                {
                    return Results.BadRequest(new ErrorResponse
                    {
                        Message = "User not found"
                    });
                }

                var user = await userService.GetByIdAsync(id);
                {
                    return Results.Ok(user);
                }
            })
            .RequireAuthorization("Admin");
        app.MapDelete("Api/Users/{id}", async (int id, UserService userService) =>
            {
                var user = await userService.DeleteAsync(id);
                if (!user)
                {
                    return Results.NotFound(new ErrorResponse
                    {
                        Message = "User not found"
                    });
                }

                return Results.NoContent();
            })
            .RequireAuthorization("Admin");
    }

    public static string? ValidateUpdate(UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email is required";
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required";
        }

        return null;
    }
}