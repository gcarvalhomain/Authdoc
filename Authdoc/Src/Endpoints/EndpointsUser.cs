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
                return BadRequest("User not found");
            }

            return Results.Ok(user);
        })
        .RequireAuthorization("Admin");
        app.MapPut("Api/Users/{id}", async (int id, UpdateUserRequest request, UserService userService) =>
            {
                var validationError = ValidateUpdate(request);
                if (validationError is not null)
                {
                    return BadRequest(validationError);
                }

                var userExist = await userService.EmailBelongsToAnotherUserAsync(id, request.Email);
                if (userExist)
                {
                    return BadRequest("Email is already in use");
                }

                var update = await userService.UpdateAsync(id, request);
                if (!update)
                {
                    return BadRequest("User not found");
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
                    return BadRequest("User not found");
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

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required";
        }

        return null;
    }

    private static IResult BadRequest(string error)
    {
        return Results.BadRequest(new ErrorResponse
        {
            Message = error
        });
    }
}