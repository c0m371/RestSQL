using System;
using System.ComponentModel;
using System.Text.Json.Nodes;
using RestSQL.Application.Interfaces;
using RestSQL.Config;

namespace RestSQL.Application;

public class ResultAggregator : IResultAggregator
{
    public JsonNode? Aggregate(IDictionary<string, IEnumerable<dynamic>> queryResults, OutputField jsonStructure)
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

    private JsonArray ProcessArray(IDictionary<string, IEnumerable<dynamic>> allQueryResults, OutputField field, object? linkValue)
    {
        if (field.QueryName == null)
            throw new ArgumentException("Array field must have query name defined", nameof(field));

        if (!allQueryResults.TryGetValue(field.QueryName, out var queryResult))
            throw new ArgumentException($"Query result {field.QueryName} not found", nameof(field));

        if (field.LinkColumn != null)
            queryResult = queryResult.Where(q => q[field.LinkColumn] == linkValue);

        var allResults = queryResult.Select(r => ProcessRow(allQueryResults, r, field)).ToList();
        var jsonArray = new JsonArray();

        foreach (var result in allResults)
            jsonArray.Add(result);

        return jsonArray;
    }

    private JsonObject ProcessRow(IDictionary<string, IEnumerable<dynamic>> allQueryResults, dynamic result, OutputField field)
    {
        if (field.Type == OutputFieldType.Object)
        {
            if (field.Fields == null)
                throw new ArgumentException("Object type must have fields defined", nameof(field));

            var jsonObject = new JsonObject();
            foreach (var subField in field.Fields)
            {
                if (subField.Name == null)
                    throw new ArgumentException("Field name cannot be null", nameof(field));

                if (subField.IsArray)
                    jsonObject.Add(subField.Name, ProcessArray(allQueryResults, subField, result[subField.LinkColumn]));
                else
                    jsonObject.Add(subField.Name, ProcessRow(allQueryResults, result, subField));
            }

            return jsonObject;
        }
        else
        {
            return GetPrimitiveValue(result, field);
        }
    }

    private object GetPrimitiveValue(dynamic result, OutputField field)
    {
        var columnName = field.ColumnName
            ?? throw new ArgumentException($"ColumnName must be defined for primitive type {field.Type}", nameof(field));

        // NOTE: Accessing result[columnName] with dynamic will throw a RuntimeBinderException
        // if the column doesn't exist, which is generally acceptable for this scenario.
        var rawValue = result[columnName];

        return field.Type switch
        {
            OutputFieldType.Long => Convert.ToInt64(rawValue),
            OutputFieldType.Decimal => Convert.ToDecimal(rawValue),
            OutputFieldType.String => Convert.ToString(rawValue),
            OutputFieldType.Date => Convert.ToDateTime(rawValue),
            OutputFieldType.Boolean => Convert.ToBoolean(rawValue),
            _ => throw new ArgumentException($"Unsupported primitive type {field.Type}", nameof(field))
        };
    }
}
