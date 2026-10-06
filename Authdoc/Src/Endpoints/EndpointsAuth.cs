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
                var validationError = ValidateRegister(request);
                if (validationError is not null)
                {
                    return Results.BadRequest(validationError);
                }

                if (request.Password != request.ConfirmationPassword)
                {
                    return Results.BadRequest(ApiErrors.PasswordsDoNotMatch);
                }

                var user = await authService.RegisterAsync(request);
                if (user is null)
                {
                    return Results.Conflict(ApiErrors.EmailAlreadyInUse);
                }

                return Results.Created($"/api/users/{user.Id}", user);
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
                return Results.Json(ApiErrors.InvalidCredentials, statusCode: StatusCodes.Status401Unauthorized);
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

    private static ErrorResponse? ValidateRegister(RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return ApiErrors.EmailRequired;
        }

        if (!EmailValidator.IsValid(request.Email))
        {
            return ApiErrors.EmailInvalid;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(request.Gender))
        {
            return ApiErrors.GenderRequired;
        }

        if (request.Age < 18)
        {
            return ApiErrors.AgeTooLow;
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            return ApiErrors.PasswordRequired;
        }

        if (request.Password.Length < 6)
        {
            return ApiErrors.PasswordTooShort;
        }

        return null;
    }

    private static ErrorResponse? ValidateLogin(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return ApiErrors.EmailRequired;
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiErrors.PasswordRequired;
        }

        return null;
    }
}
