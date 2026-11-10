namespace Authdoc.Models.Entities;

public class Document
{
    public const int TypeMaxLength = 50;
    public const int NameMaxLength = 100;
    public const int NumberMaxLength = 50;
    public const int DescriptionMaxLength = 500;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}