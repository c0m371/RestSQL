using Microsoft.Extensions.Logging;
using RestSQL.Application.Interfaces;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RestSQL.Application;

public class EndpointService(IQueryDispatcher queryDispatcher, IResultAggregator resultAggregator, IRequestBodyParser requestBodyParser, ILogger<EndpointService> logger) : IEndpointService
{
    public async Task<JsonNode?> GetEndpointResultAsync(Endpoint endpoint, IDictionary<string, object?> parameterValues, Stream? body)
    {
        await ExecuteWriteOperations(endpoint, parameterValues, body).ConfigureAwait(false);
        return await ExecuteQueries(endpoint, parameterValues).ConfigureAwait(false);
    }

    private async Task ExecuteWriteOperations(Endpoint endpoint, IDictionary<string, object?> parameterValues, Stream? body)
    {
        if (!endpoint.WriteOperations.Any())
            return;

        (var parsedBody, var stringBody) = await requestBodyParser.ReadAndParseJsonStreamAsync(body).ConfigureAwait(false);

        var transactions = new Dictionary<string, ITransaction>();
        try
        {
            transactions = BeginTransactions(endpoint);

            foreach (var writeOperation in endpoint.WriteOperations)
            {
                var capturedOutput = await ProcessWriteOperation(parsedBody, stringBody, transactions, writeOperation, parameterValues).ConfigureAwait(false);

                foreach (var output in capturedOutput)
                    if (!parameterValues.TryAdd(output.Key, output.Value))
                        throw new InvalidOperationException($"Cannot add captured output '{output.Key}' to parameters. A parameter with the same name already exists.");
            }

            CommitTransactions(transactions);
        }
        catch
        {
            RollbackTransactions(transactions);
            throw;
        }
        finally
        {
            DisposeTransactions(transactions);
        }
    }

    private static async Task<IDictionary<string, object?>> ProcessWriteOperation(JsonNode? parsedBody, string? stringBody, Dictionary<string, ITransaction> transactions, WriteOperation writeOperation, IDictionary<string, object?> parameterValues)
    {
        var transaction = transactions[writeOperation.ConnectionName];

        var parametersForWriteOperation = new Dictionary<string, object?>(parameterValues);

        MergeBodyParameters(parsedBody, stringBody, writeOperation, parametersForWriteOperation);

        if (writeOperation.OutputCaptures.Any())
        {
            var result = await transaction.ExecuteQueryAsync(writeOperation.Sql, parametersForWriteOperation).ConfigureAwait(false);
            var capturedOutput = new Dictionary<string, object?>();

            foreach (var capture in writeOperation.OutputCaptures)
            {
                if (!result.TryGetValue(capture.ColumnName, out var value))
                    throw new InvalidOperationException($"Column name {capture.ColumnName} not found in result");

                capturedOutput.Add(capture.ParameterName, value);
            }

            return capturedOutput;
        }

        await transaction.ExecuteNonQueryAsync(writeOperation.Sql, parametersForWriteOperation).ConfigureAwait(false);
        return new Dictionary<string, object?>();
    }

    private static void MergeBodyParameters(JsonNode? parsedBody, string? stringBody, WriteOperation writeOperation, Dictionary<string, object?> parametersForWriteOperation)
    {
        if (writeOperation.BodyType == WriteOperationBodyType.Raw)
        {
            if (writeOperation.RawBodyParameterName is null)
                throw new InvalidOperationException("Cannot use body as parameter value if no BodyParameterName is provided");

            if (!parametersForWriteOperation.TryAdd(writeOperation.RawBodyParameterName, stringBody))
                throw new InvalidOperationException($"Cannot add body parameter '{writeOperation.RawBodyParameterName}' to parameters for write operation. A parameter with the same name already exists.");
        }
        else if (writeOperation.BodyType == WriteOperationBodyType.Object)
        {
            if (parsedBody is not JsonObject bodyAsJsonObject)
                throw new InvalidOperationException("Cannot use body as parameter value if body is not a JSON object");

            foreach (var kvp in bodyAsJsonObject)
            {
                object? clrValue = null;

                if (kvp.Value is JsonValue jsonValue)
                    clrValue = GetClrValue(jsonValue);

                if (!parametersForWriteOperation.TryAdd(kvp.Key, clrValue))
                    throw new InvalidOperationException($"Cannot add body parameter '{kvp.Key}' to parameters for write operation. A parameter with the same name already exists.");
            }
        }
    }

    private static object? GetClrValue(JsonValue jsonValue)
    {
        object? clrValue;
        clrValue = jsonValue.GetValueKind() switch
        {
            JsonValueKind.String => jsonValue.GetValue<string>(),
            JsonValueKind.Number => jsonValue.GetValue<long>(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
        return clrValue;
    }

    private void CommitTransactions(Dictionary<string, ITransaction> transactions)
    {
        foreach (var kvp in transactions)
        {
            kvp.Value.Commit();
            logger.LogInformation("Commited transaction {name}", kvp.Key);
        }
    }

    private void DisposeTransactions(Dictionary<string, ITransaction> transactions)
    {
        foreach (var kvp in transactions)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error while disposing transaction {name}", kvp.Key);
            }
        }
    }

    private void RollbackTransactions(Dictionary<string, ITransaction> transactions)
    {
        foreach (var kvp in transactions)
        {
            try
            {
                kvp.Value.Rollback();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error while rolling back transaction {name}", kvp.Key);
            }
        }
    }

    private Dictionary<string, ITransaction> BeginTransactions(Endpoint endpoint)
    {
        var transactions = new Dictionary<string, ITransaction>();

        foreach (var connectionName in endpoint.WriteOperations.Select(o => o.ConnectionName).Distinct())
        {
            var transaction = queryDispatcher.BeginTransaction(connectionName);
            transactions.Add(connectionName, transaction);
        }

        return transactions;
    }

    private async Task<JsonNode?> ExecuteQueries(Endpoint endpoint, IDictionary<string, object?> parameterValues)
    {
        var queryTasks = endpoint.SqlQueries
                    .Select(async q => new { Name = q.Key, Result = await queryDispatcher.QueryAsync(q.Value.ConnectionName, q.Value.Sql, parameterValues).ConfigureAwait(false) });
        var taskResults = await Task.WhenAll(queryTasks).ConfigureAwait(false);
        var queryResults = taskResults.ToDictionary(r => r.Name, r => r.Result);

        return resultAggregator.Aggregate(queryResults, endpoint.OutputStructure);
    }

}
