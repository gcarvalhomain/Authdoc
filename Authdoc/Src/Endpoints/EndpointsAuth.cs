using System.Text.RegularExpressions;
using Authdoc.Application.DTOs;
using Authdoc.Application.Services;
using Authdoc.Responses;
using Microsoft.IdentityModel.Tokens.Experimental;

namespace Authdoc.Endpoints;

public static class EndpointsAuth
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("Api/Auth/{id}", async (RegisterUserRequest request, AuthService authService) =>
        {
            var validationError = ValidateRegister(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Message = validationError
                });
            }
            return null;
        });
    }

    public static string? ValidateRegister(RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required";
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return "Password is required";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email is required";
        }

        if (request.Age <= 18)
        {
            return "Age must be 18 years old";
        }
        if(request.Password.Length < 6)
        {
          return "Password must be at least 6 characters long";  
        }

        if (!Regex.IsMatch(request.Password, "[^a-zA-Z0-9]") ||
            !Regex.IsMatch(request.Email, "[0-9]") ||
            !Regex.IsMatch(request.Password, "[!@#$%^&*(),.?\\\":{}|<>]"))
        {
            return "Password must consist only of letters and digits";
        }
        return null;
    }
}