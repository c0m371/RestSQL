namespace RestSQL.Config;

public record Parameter(
    ParameterType Type,
    ParameterDataType DataType,
    string Value
);
