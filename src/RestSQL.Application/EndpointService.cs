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
        logger.LogDebug("Handling {method} {path} (status {status}) with {paramCount} initial parameters",
            endpoint.Method, endpoint.Path, endpoint.StatusCode, parameterValues.Count);

        try
        {
            await ExecuteWriteOperations(endpoint, parameterValues, body).ConfigureAwait(false);
            var result = await ExecuteQueries(endpoint, parameterValues).ConfigureAwait(false);
            logger.LogDebug("Finished handling {method} {path}", endpoint.Method, endpoint.Path);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while handling endpoint {method} {path}", endpoint.Method, endpoint.Path);
            throw;
        }
    }

    private async Task ExecuteWriteOperations(Endpoint endpoint, IDictionary<string, object?> parameterValues, Stream? body)
    {
        if (!endpoint.WriteOperations.Any())
        {
            logger.LogDebug("No write operations for endpoint {path}", endpoint.Path);
            return;
        }

        var parsedBody = await requestBodyParser.ReadAndParseJsonStreamAsync(body).ConfigureAwait(false);
        logger.LogDebug("Parsed request body: {hasBody}", parsedBody is not null);

        var transactions = new Dictionary<string, ITransaction>();
        try
        {
            transactions = BeginTransactions(endpoint);
            logger.LogDebug("Began transactions for connections: {connections}", string.Join(',', transactions.Keys));

            foreach (var writeOperation in endpoint.WriteOperations)
            {
                logger.LogDebug("Executing write operation on connection={conn} sql={sql}", writeOperation.ConnectionName, writeOperation.Sql);
                var capturedOutput = await ProcessWriteOperation(parsedBody, transactions, writeOperation, parameterValues).ConfigureAwait(false);

                if (capturedOutput.Any())
                {
                    logger.LogDebug("Captured output parameters: {keys}", string.Join(',', capturedOutput.Keys));
                }

                foreach (var output in capturedOutput)
                    if (!parameterValues.TryAdd(output.Key, output.Value))
                        throw new InvalidOperationException($"Cannot add captured output '{output.Key}' to parameters. A parameter with the same name already exists.");
            }

            CommitTransactions(transactions);
            logger.LogDebug("Committed transactions for endpoint {path}", endpoint.Path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during write operations for endpoint {path}. Initiating rollback.", endpoint.Path);
            RollbackTransactions(transactions);
            throw;
        }
        finally
        {
            DisposeTransactions(transactions);
            logger.LogDebug("Disposed transactions for endpoint {path}", endpoint.Path);
        }
    }

    private static async Task<IDictionary<string, object?>> ProcessWriteOperation(JsonNode? parsedBody, Dictionary<string, ITransaction> transactions, WriteOperation writeOperation, IDictionary<string, object?> parameterValues)
    {
        var transaction = transactions[writeOperation.ConnectionName];

        var parametersForWriteOperation = new Dictionary<string, object?>(parameterValues);

        MergeBodyParameters(parsedBody, writeOperation, parametersForWriteOperation);

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

    private static void MergeBodyParameters(JsonNode? parsedBody, WriteOperation writeOperation, Dictionary<string, object?> parametersForWriteOperation)
    {
        if (writeOperation.BodyType == WriteOperationBodyType.Value)
        {
            if (writeOperation.ValueParameterName is null)
                throw new InvalidOperationException("Cannot use body as parameter value if no ValueParameterName is provided");

            if (!parametersForWriteOperation.TryAdd(writeOperation.ValueParameterName, GetClrValue(parsedBody?.AsValue())))
                throw new InvalidOperationException($"Cannot add body parameter '{writeOperation.ValueParameterName}' to parameters for write operation. A parameter with the same name already exists.");
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

    private static object? GetClrValue(JsonValue? jsonValue)
    {
        if (jsonValue is null)
            return null;

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
            logger.LogDebug("Commited transaction {name}", kvp.Key);
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
            logger.LogDebug("Beginning transaction for connection {connection}", connectionName);
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

        if (endpoint.OutputStructure is null)
            return null;

        return resultAggregator.Aggregate(queryResults, endpoint.OutputStructure);
    }

}
