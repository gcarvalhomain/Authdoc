using Authdoc.Responses;
using Authdoc.Application.Services;
using Azure.Core;


namespace Authdoc.Endpoints;

public static class AuthenticateUser
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("Api/Auth/Get/{id}", async (int id, UserService userService) =>
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
        app.MapPost("Api/Auth/Post", async (RegisterUserRequest userRequest, UserService userService) =>
        {
            var validationError = ValidateUser(userRequest.Name, userRequest.Email, userRequest.Age);
            if (validationError is not null)
            {
                return Results.BadRequest(new ErrorResponse()
                {
                    Message = validationError,
                });
            }
            
            var user = await userService.CreateAsync(userRequest);
            
            return Results.Created($"/api/auth/{user.Id}", user);
        });
    }

    public static string? ValidateUser(string? name, string email, int age)
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
