using System;

namespace Authdoc.Application.DTOs;

public sealed record LoginResponse
{
    public required UserResponse User { get; init; }
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime RefreshTokenExpiresAt { get; init; }
}