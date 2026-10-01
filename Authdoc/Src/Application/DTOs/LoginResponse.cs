using System;

namespace Authdoc.Application.DTOs;

public sealed record LoginResponse
{
    public required UserResponse User { get; init; }
    public required string Token { get; init; }
    public DateTime ExpiresAt { get; init; }
    
}