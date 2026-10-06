using System;
using System.Security.Claims;
using Authdoc.Application.DTOs;
using Authdoc.Application.Services;
using Authdoc.Responses;
using Authdoc.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Authdoc.Endpoints;

public static class EndpointsAuth
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (RegisterUserRequest request, AuthService authService) =>
            {
                request.Email = EmailValidator.Normalize(request.Email);
                if (!EmailValidator.IsValid(request.Email))
                {
                    return Results.BadRequest("Email is invalid");
                }

                var validationError = ValidateRegister(request);
                if (validationError is not null)
                {
                    return Results.BadRequest(validationError);
                }

                if (request.Password != request.ConfirmationPassword)
                {
                    return Results.BadRequest("Passwords do not match");
                }

                var user = await authService.RegisterAsync(request);
                if (user is null)
                {
                    return Results.Conflict("Email already in use");
                }

                return Results.Created($"/api/auth/me", user);
            })
            .RequireAuthorization("Admin");
        app.MapPost("/api/auth/login", async (LoginRequest request, AuthService authService) =>
        {
            request.Email = EmailValidator.Normalize(request.Email);
            var validationError = ValidateLogin(request);
            if (validationError is not null)
            {
                return Results.BadRequest(validationError);
            }

            var loginResponse = await authService.LoginAsync(request);
            if (loginResponse is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(loginResponse);
        });

        app.MapGet("/api/auth/me", (ClaimsPrincipal user) =>
            {
                var id = user.FindFirst(ClaimTypes.NameIdentifier);
                var name = user.FindFirst(ClaimTypes.Name);
                var email = user.FindFirst(ClaimTypes.Email);

                return Results.Ok(new
                {
                    Id = id?.Value,
                    Name = name?.Value,
                    Email = email?.Value
                });
            })
            .RequireAuthorization();
    }

    private static string? ValidateRegister(RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required";
        }

        if (string.IsNullOrWhiteSpace(request.Gender))
        {
            return "Gender is required";
        }

        if (request.Age < 18)
        {
            return "Age must be 18 years of age or older";
        }

        if (request.Password.Length < 6)
        {
            return "Password must be at least 6 characters long";
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

    private static IResult BadRequest(string error)
    {
        return Results.BadRequest(new ErrorResponse
        {
            Message = error
        });
    }
}