namespace RestSQL.Domain;

public record WriteOperation
{
    public required string ConnectionName { get; init; }
    public required string Sql { get; init; }
    public IList<OutputCapture> OutputCaptures { get; init; } = [];
    public bool UseRawBodyValue { get; init; }
    public string? BodyParameterName { get; init; }
    public string? JsonPath { get; init; }
    public IList<WriteOperation> WriteOperations { get; init; } = [];
}
