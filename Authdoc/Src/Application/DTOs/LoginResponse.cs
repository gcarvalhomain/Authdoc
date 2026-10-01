using System;

namespace Authdoc.Application.DTOs;

public sealed record LoginResponse
{
    public UserResponse User { get; init; } = null!;
    public string? Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    
}