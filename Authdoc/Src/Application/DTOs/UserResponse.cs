using System;

namespace Authdoc.Application.DTOs;

public sealed record UserResponse
{
    
    
    public required int Age { get; init; }
    public required Guid Id { get; init; }
    public required string Email { get; init; } 
    public required string Gender { get; init; } 
    public required string Name { get; init; } 
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    
    

}