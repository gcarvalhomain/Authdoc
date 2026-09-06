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
                var validationError = ValidateUser(request.Name, request.Email, request.Age);
                if (validationError is not null)
                {
                    return Results.BadRequest(new ErrorResponse
                    {
                        Message = validationError
                    });
                }

                var emailExists = await userService.EmailExistAsync(request.Email);
                if (emailExists)
                {
                    return Results.Conflict(new ErrorResponse
                    {
                        Message = "Email already exists"
                    });
                }

                var userExist = await userService.UserExistAsync(id, request.Email);
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

    static string? ValidateUser(string? name, string email, int age)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return "Email is required";
        }

        if (age <= 0)
        {
            return "Age is required";
        }

        return null;
    }
}