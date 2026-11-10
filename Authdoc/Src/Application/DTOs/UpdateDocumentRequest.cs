namespace Authdoc.Application.DTOs;

public class UpdateDocumentRequest
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
}