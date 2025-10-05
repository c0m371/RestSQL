namespace RestSQL.Domain;

public record Endpoint
{
    public required string Path { get; init; }
    public required string Verb { get; init; }
    public required int StatusCode { get; init; }
    public required Dictionary<string, SqlQuery> SqlQueries { get; init; }
    public required OutputField OutputStructure { get; init; }
};
