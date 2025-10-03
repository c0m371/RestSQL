using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using RestSQL.Application.Interfaces;
using RestSQL.Config;

namespace RestSQL.Application;

public class ResultAggregator : IResultAggregator
{
    public JsonNode? Aggregate(
        IDictionary<string, IEnumerable<IDictionary<string, object?>>> queryResults,
        OutputField jsonStructure)
    {
        if (jsonStructure.IsArray)
            return ProcessArray(queryResults, jsonStructure, null);

        if (jsonStructure.QueryName == null)
            throw new ArgumentException("Root object has no query name defined", nameof(jsonStructure));

        if (!queryResults.TryGetValue(jsonStructure.QueryName, out var queryResult))
            throw new ArgumentException($"Query result {jsonStructure.QueryName} not found", nameof(jsonStructure));

        var firstResult = queryResult.FirstOrDefault();
        if (firstResult == null)
            return null;

        return ProcessRow(queryResults, firstResult, jsonStructure);
    }

    private JsonArray ProcessArray(
        IDictionary<string, IEnumerable<IDictionary<string, object?>>> allQueryResults,
        OutputField field,
        object? linkValue)
    {
        if (field.QueryName == null)
            throw new ArgumentException($"Array field '{field.Name ?? "<unnamed>"}' must have QueryName defined", nameof(field));

        if (!allQueryResults.TryGetValue(field.QueryName, out var queryResult))
            throw new ArgumentException($"Query result {field.QueryName} not found", nameof(field));

        if (field.LinkColumn != null)
            queryResult = queryResult.Where(q => GetValue(q, field.LinkColumn) == linkValue);

        return new JsonArray(queryResult.Select(r => ProcessRow(allQueryResults, r, field)).ToArray());
    }

    private JsonNode? ProcessRow(
        IDictionary<string, IEnumerable<IDictionary<string, object?>>> allQueryResults,
        IDictionary<string, object?> result,
        OutputField field)
    {
        if (field.Type == OutputFieldType.Object)
        {
            if (field.Fields == null)
                throw new ArgumentException($"Object field '{field.Name ?? "<unnamed>"}' must have Fields defined", nameof(field));

            var jsonObject = new JsonObject();

            foreach (var subField in field.Fields)
            {
                if (subField.Name == null)
                    throw new ArgumentException("Field name cannot be null", nameof(subField));

                if (subField.IsArray)
                {
                    jsonObject[subField.Name] = ProcessArray(
                        allQueryResults,
                        subField,
                        subField.LinkColumn != null ? GetValue(result, subField.LinkColumn) : null
                    );
                }
                else
                {
                    jsonObject[subField.Name] = ProcessRow(allQueryResults, result, subField);
                }
            }

            return jsonObject;
        }

        // Primitive case (can appear at root or inside an array)
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
        => row.TryGetValue(columnName, out var value) ? value : null;
}
