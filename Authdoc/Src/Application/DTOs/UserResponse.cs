using System;

namespace Authdoc.Application.DTOs;

public sealed record UserResponse
{
    
    
    public int Age { get; init; }
    public Guid Id { get; init; }
    public required string Email { get; init; } 
    public required string Gender { get; init; } 
    public required string Name { get; init; } 
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    
    

}