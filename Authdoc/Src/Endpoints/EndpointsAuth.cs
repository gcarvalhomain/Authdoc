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
        })
        .RequireRateLimiting("login");

        app.MapPut("/api/auth/password", async (ChangePasswordRequest request, ClaimsPrincipal currentUser, AuthService authService) =>
            {
                var validationError = ValidateChangePassword(request);
                if (validationError is not null)
                {
                    return Results.BadRequest(validationError);
                }

                var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(currentUserId, out var userId))
                {
                    return Results.Json(ApiErrors.Unauthorized, statusCode: StatusCodes.Status401Unauthorized);
                }

                var result = await authService.ChangePasswordAsync(userId, request);
                if (result == ChangePasswordResult.UserNotFound)
                {
                    return Results.NotFound(ApiErrors.UserNotFound);
                }

                if (result == ChangePasswordResult.WrongCurrentPassword)
                {
                    return Results.BadRequest(ApiErrors.CurrentPasswordIncorrect);
                }

                return Results.NoContent();
            })
            .RequireAuthorization()
            .RequireRateLimiting("login");

        app.MapPost("/api/auth/refresh", async (RefreshTokenRequest request, AuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest(ApiErrors.RefreshTokenRequired);
            }

            var loginResponse = await authService.RefreshAsync(request.RefreshToken);
            if (loginResponse is null)
            {
                return Results.Json(ApiErrors.InvalidRefreshToken, statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(loginResponse);
        });

        app.MapGet("/api/auth/me", (ClaimsPrincipal user) =>
            {
                var id = user.FindFirst(ClaimTypes.NameIdentifier);
                var name = user.FindFirst(ClaimTypes.Name);
                var email = user.FindFirst(ClaimTypes.Email);
                var role = user.FindFirst(ClaimTypes.Role);

                return Results.Ok(new
                {
                    Id = id?.Value,
                    Name = name?.Value,
                    Email = email?.Value,
                    Role = role?.Value
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

    private static ErrorResponse? ValidateChangePassword(ChangePasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.CurrentPassword))
        {
            return ApiErrors.CurrentPasswordRequired;
        }

        if (string.IsNullOrEmpty(request.NewPassword))
        {
            return ApiErrors.PasswordRequired;
        }

        if (request.NewPassword.Length < 6)
        {
            return ApiErrors.PasswordTooShort;
        }

        if (request.NewPassword != request.ConfirmationPassword)
        {
            return ApiErrors.PasswordsDoNotMatch;
        }

        if (request.NewPassword == request.CurrentPassword)
        {
            return ApiErrors.NewPasswordSameAsCurrent;
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
