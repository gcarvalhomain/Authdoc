using System;

namespace Authdoc.Application.DTOs;

public sealed record LoginResponse
{
    public UserResponse User { get; set; } = null!;
    public string? Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    
}