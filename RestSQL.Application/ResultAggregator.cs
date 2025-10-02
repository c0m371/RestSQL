using System;
using System.ComponentModel;
using System.Text.Json.Nodes;
using RestSQL.Application.Interfaces;
using RestSQL.Config;

namespace RestSQL.Application;

public class ResultAggregator : IResultAggregator
{
    public object? Aggregate(IDictionary<string, IEnumerable<dynamic>> queryResults, OutputField jsonStructure)
    {
        if (jsonStructure.IsArray)
            return ProcessArray(queryResults, queryResults[jsonStructure.QueryName], jsonStructure.Type, jsonStructure.Fields);


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
                if (subField.IsArray)
                    jsonObject.Add(subField.Name, ProcessArray(allQueryResults, field));
                else
                    jsonObject.Add(subField.Name, ProcessRow(allQueryResults, result, field));
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
