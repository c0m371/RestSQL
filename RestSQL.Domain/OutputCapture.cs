namespace RestSQL.Domain;

public record OutputCapture
{
    public required string ColumnName { get; init; }
    public required string ParameterName { get; init; }
}