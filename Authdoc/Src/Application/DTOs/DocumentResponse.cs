using Authdoc.Models.Entities;

namespace Authdoc.Application.DTOs;

public sealed record DocumentResponse
{
    public required Guid Id { get; init; }
    public required string Type { get; init; }
    public required string Name { get; init; }
    public required string Number { get; init; }
    public required string? Description { get; init; }
    public required DateOnly IssuedAt { get; init; }
    public required DateOnly? ExpiresAt { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }

    public static DocumentResponse From(Document document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            Type = document.Type,
            Name = document.Name,
            Number = document.Number,
            Description = document.Description,
            IssuedAt = document.IssuedAt,
            ExpiresAt = document.ExpiresAt,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }
}
