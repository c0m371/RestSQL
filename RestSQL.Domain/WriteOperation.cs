namespace RestSQL.Domain;

public record WriteOperation
{
    public required string ConnectionName { get; init; }
    public required string Sql { get; init; }
    public string? CaptureKey { get; init; }
    public string? SourceArray { get; init; }
}
