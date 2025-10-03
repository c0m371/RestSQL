namespace RestSQL.Config;

public record OutputField(
    OutputFieldType Type,
    bool IsArray,
    string? Name,
    string? ColumnName,
    string? QueryName,
    string? LinkColumn,
    IList<OutputField>? Fields
);

public enum OutputFieldType
{
    Long,
    Decimal,
    String,
    Date,
    Boolean,
    Object
}