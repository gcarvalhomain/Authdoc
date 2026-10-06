namespace Authdoc.Responses;

public sealed record ErrorResponse
{
    public required string Code { get; init; }
    public required string Message { get; init; }
}
