using System.Security.Claims;
using System.Text.RegularExpressions;
using Authdoc.Application.DTOs;
using Authdoc.Application.Services;
using Authdoc.Responses;

namespace Authdoc.Endpoints;

public static class EndpointsAuth
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("Api/Auth/Register", async (RegisterUserRequest request, AuthService authService) =>
        {
            var validationError = ValidateRegister(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Message = validationError
                });
            }

            var user = await authService.RegisterAsync(request);
            if (user is null)
            {
                return Results.Conflict(new ErrorResponse
                {
                    Message = "Email already exists"
                });
            }

            return Results.Created($"/api/auth/{user.Id}", user);
        });
        app.MapPost("Api/Auth/login", async (LoginRequest request, AuthService authService) =>
        {
            var validationError = ValidateLogin(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Message = validationError
                });
            }

            var loginResponse = await authService.LoginAsync(request);
            if (loginResponse is null)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Message = "Username or password is incorrect"
                });
            }

            return Results.Ok(loginResponse);
        });
        app.MapGet("Api/Auth/Me", (ClaimsPrincipal user) =>
        {
            
            var id = user.FindFirst(ClaimTypes.NameIdentifier);
            var name = user.FindFirst(ClaimTypes.Name);
            var email = user.FindFirst(ClaimTypes.Email);

            return Results.Ok(new
            {
                Id = id,
                Name = name,
                Email = email
            });
        })
        .RequireAuthorization();
    }

    public static string? ValidateRegister(RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required";
        }

        if (string.IsNullOrWhiteSpace(request.PasswordHash))
        {
            return "Password is required";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email is required";
        }

        if (string.IsNullOrWhiteSpace(request.Gender))
        {
            return "Gender is required";
        }

        if (request.Age <= 18)
        {
            return "Age must be 18 years old";
        }
        if(request.PasswordHash.Length < 6)
        {
          return "Password must be at least 6 characters long";  
        }

        if (!Regex.IsMatch(request.PasswordHash, "[^a-zA-Z0-9]") ||
            !Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") ||
            !Regex.IsMatch(request.PasswordHash, "[!@#$%^&*(),.?\\\":{}|<>]"))
        {
            return "Password must consist only of letters and digits";
        }
        return null;
    }

    public static string? ValidateLogin(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email  is required";
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return "Password is required";
        }
        return null;
    }
}