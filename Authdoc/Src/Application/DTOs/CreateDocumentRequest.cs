namespace Authdoc.Application.DTOs;

public class CreateDocumentRequest
{
    public required string Type { get; set; }
    public required string Name { get; set; }
    public required string Number { get; set; }
    public string? Description { get; set; }
    public DateOnly IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
}