namespace RestSQL.Domain;

public record WriteOperation
{
    public required string ConnectionName { get; init; }
    public required string Sql { get; init; }
    public IList<OutputCapture> OutputCaptures { get; init; } = [];
    public WriteOperationBodyType BodyType { get; set; }
    public string? RawBodyParameterName { get; init; }
}

public enum WriteOperationBodyType
{
    None,
    Raw,
    Object
}