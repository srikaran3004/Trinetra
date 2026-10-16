namespace Trinetra.API.DTOs;

public record PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int Total { get; init; }
    public int Limit { get; init; }
    public int Offset { get; init; }
}

public record ErrorResponse
{
    public ErrorDetail Error { get; init; } = new();
}

public record ErrorDetail
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int Status { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? RequestId { get; init; }
}
