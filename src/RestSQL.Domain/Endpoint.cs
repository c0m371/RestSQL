namespace RestSQL.Domain;

public record Endpoint
{
    public required string Path { get; init; }
    public required string Method { get; init; }
    public required int StatusCode { get; init; }
    public int? StatusCodeOnEmptyResult { get; init; }
    public bool Authorize { get; init; }
    public string? AuthorizationScope { get; init; }
    public IDictionary<string, SqlQuery> SqlQueries { get; init; } = new Dictionary<string, SqlQuery>();
    public required OutputField? OutputStructure { get; init; }
    public IList<WriteOperation> WriteOperations { get; init; } = [];
}
