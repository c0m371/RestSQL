using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using RestSQL.Application.Interfaces;
using RestSQL.Domain;

namespace RestSQL.Application;

public class ResultAggregator(ILogger<ResultAggregator> logger) : IResultAggregator
{
    public JsonNode? Aggregate(
        IDictionary<string, IEnumerable<IDictionary<string, object?>>> queryResults,
        OutputField jsonStructure)
    {
        logger.LogDebug("Aggregating results for structure: {type} isArray={isArray} queryName={q}", jsonStructure.Type, jsonStructure.IsArray, jsonStructure.QueryName);

        if (jsonStructure.IsArray)
            return ProcessArray(queryResults, jsonStructure, null);

        if (jsonStructure.QueryName is null)
        {
            logger.LogError("Root object has no QueryName defined");
            throw new ArgumentException("Root object has no QueryName defined", nameof(jsonStructure));
        }

        if (!queryResults.TryGetValue(jsonStructure.QueryName, out var queryResult))
        {
            logger.LogError("Query result '{name}' not found", jsonStructure.QueryName);
            throw new ArgumentException($"Query result '{jsonStructure.QueryName}' not found", nameof(jsonStructure));
        }

        var firstResult = queryResult.FirstOrDefault();
        if (firstResult is null)
        {
            logger.LogDebug("No rows returned for root query '{name}', returning null", jsonStructure.QueryName);
            return null;
        }

        return ProcessRow(queryResults, firstResult, jsonStructure);
    }

    private JsonArray ProcessArray(
        IDictionary<string, IEnumerable<IDictionary<string, object?>>> allQueryResults,
        OutputField field,
        object? linkValue)
    {
        if (field.QueryName is null)
            throw new ArgumentException($"Array field '{field.Name ?? "<unnamed>"}' must have QueryName defined", nameof(field));

        if (!allQueryResults.TryGetValue(field.QueryName, out var queryResult))
            throw new ArgumentException($"Query result '{field.QueryName}' not found", nameof(field));

        if (field.LinkColumn is not null)
            queryResult = queryResult.Where(q => Equals(GetValue(q, field.LinkColumn), linkValue));

        var jsonArray = new JsonArray();
        foreach (var row in queryResult)
            jsonArray.Add(ProcessRow(allQueryResults, row, field));
        return jsonArray;
    }

    private JsonNode? ProcessRow(
        IDictionary<string, IEnumerable<IDictionary<string, object?>>> allQueryResults,
        IDictionary<string, object?> result,
        OutputField field)
    {
        if (field.Type == OutputFieldType.Object)
        {
            if (field.Fields is null)
                throw new ArgumentException($"Object field '{field.Name ?? "<unnamed>"}' must have Fields defined", nameof(field));

            var jsonObject = new JsonObject();

            foreach (var subField in field.Fields)
            {
                if (subField.IsArray)
                {
                    if (subField.Name is null)
                        throw new ArgumentException("Array fields inside objects must have a Name defined", nameof(subField));

                    jsonObject[subField.Name] =
                        ProcessArray(
                            allQueryResults,
                            subField,
                            subField.LinkColumn is not null ? GetValue(result, subField.LinkColumn) : null
                        );
                }
                else if (subField.Type == OutputFieldType.Object)
                {
                    if (subField.Name is null)
                        throw new ArgumentException("Object fields inside objects must have a Name defined", nameof(subField));

                    jsonObject[subField.Name] = ProcessRow(allQueryResults, result, subField);
                }
                else
                {
                    if (subField.Name is null)
                        throw new ArgumentException("Primitive fields inside objects must have a Name defined", nameof(subField));

                    jsonObject[subField.Name] = GetPrimitiveValue(result, subField);
                }
            }

            return jsonObject;
        }

        return GetPrimitiveValue(result, field);
    }

    private JsonNode? GetPrimitiveValue(IDictionary<string, object?> row, OutputField field)
    {
        var columnName = field.ColumnName
            ?? throw new ArgumentException($"ColumnName must be defined for primitive type {field.Type}", nameof(field));

        var rawValue = GetValue(row, columnName);
        if (rawValue is null or DBNull) return null;

        return field.Type switch
        {
            OutputFieldType.Long => JsonValue.Create(Convert.ToInt64(rawValue)),
            OutputFieldType.Decimal => JsonValue.Create(Convert.ToDecimal(rawValue)),
            OutputFieldType.String => JsonValue.Create(Convert.ToString(rawValue)),
            OutputFieldType.Date => JsonValue.Create(Convert.ToDateTime(rawValue)),
            OutputFieldType.Boolean => JsonValue.Create(Convert.ToBoolean(rawValue)),
            _ => throw new ArgumentException($"Unsupported primitive type {field.Type}", nameof(field))
        };
    }

    private static object? GetValue(IDictionary<string, object?> row, string columnName)
    {
        if (row.TryGetValue(columnName, out var value))
            return value;

        // Case-insensitive fallback
        var altKey = row.Keys.FirstOrDefault(k => string.Equals(k, columnName, StringComparison.OrdinalIgnoreCase));
        if (altKey is not null)
            return row[altKey];

        throw new ArgumentException($"Column '{columnName}' not found in query result row.");
    }

}
