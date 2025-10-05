namespace RestSQL.Domain;

public record OutputField
{
    public required OutputFieldType Type { get; init; }
    public bool IsArray { get; init; } = false;
    public string? Name { get; init; }
    public string? ColumnName { get; init; }
    public string? QueryName { get; init; }
    public string? LinkColumn { get; init; }
    public IList<OutputField>? Fields { get; init; }
}
