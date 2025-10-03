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
            return ProcessArray(queryResults, jsonStructure);
        
        if (jsonStructure.QueryName == null)
            throw new ArgumentException("Array field must have query name defined", nameof(jsonStructure));

        if (!queryResults.TryGetValue(jsonStructure.QueryName, out var queryResult))
            throw new ArgumentException($"Query result {jsonStructure.QueryName} not found", nameof(jsonStructure));

        var firstResult = queryResult.FirstOrDefault();

        if (firstResult == null)
            return null;

        return ProcessRow(queryResults, firstResult, jsonStructure);
    }

    private JsonArray ProcessArray(IDictionary<string, IEnumerable<dynamic>> allQueryResults, OutputField field)
    {
        if (field.QueryName == null)
            throw new ArgumentException("Array field must have query name defined", nameof(field));

        if (!allQueryResults.TryGetValue(field.QueryName, out var queryResult))
            throw new ArgumentException($"Query result {field.QueryName} not found", nameof(field));

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
                    jsonObject.Add(subField.Name, ProcessArray(allQueryResults, subField));
                else
                    jsonObject.Add(subField.Name, ProcessRow(allQueryResults, result, subField));
            }

            return jsonObject;
        }
        else if (field.Type == OutputFieldType.Long)
        {
            return Convert.ToInt64(result[field.ColumnName]);
        }
        else if (field.Type == OutputFieldType.Decimal)
        {
            return Convert.ToDecimal(result[field.ColumnName]);
        }
        else if (field.Type == OutputFieldType.String)
        {
            return Convert.ToString(result[field.ColumnName]);
        }
        else if (field.Type == OutputFieldType.Date)
        {
            return Convert.ToDateTime(result[field.ColumnName]);
        }
        else if (field.Type == OutputFieldType.Boolean)
        {
            return Convert.ToBoolean(result[field.ColumnName]);
        }
        else
        {
            throw new ArgumentException("Invalid field type", nameof(field));
        }
    }
}
