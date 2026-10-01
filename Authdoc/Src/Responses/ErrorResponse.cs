using System;

namespace Authdoc.Responses;

public sealed record ErrorResponse
{
    public string Message { get; init; } = String.Empty;
}