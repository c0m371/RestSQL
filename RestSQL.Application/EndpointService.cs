using Microsoft.Extensions.Logging;
using RestSQL.Application.Interfaces;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;
using System.Text.Json.Nodes;

namespace RestSQL.Application;

public class EndpointService(IQueryDispatcher queryDispatcher, IResultAggregator resultAggregator, IRequestBodyParser requestBodyParser, ILogger logger) : IEndpointService
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

        var parsedBody = await requestBodyParser.ReadAndParseJsonStreamAsync(body).ConfigureAwait(false);

        var transactions = new Dictionary<string, ITransaction>();
        try
        {
            transactions = await BeginTransactions(endpoint).ConfigureAwait(false);

            foreach (var writeOperation in endpoint.WriteOperations)
            {
                var capturedOutput = await ProcessWriteOperation(parsedBody, transactions, writeOperation, parameterValues).ConfigureAwait(false);

                foreach (var output in capturedOutput)
                    if (!parameterValues.TryAdd(output.Key, output.Value))
                        throw new InvalidOperationException($"Cannot add captured output '{output.Key}' to parameters. A parameter with the same name already exists.");
            }

            await CommitTransactions(transactions).ConfigureAwait(false);
        }
        catch
        {
            await RollbackTransactions(transactions).ConfigureAwait(false);

            throw;
        }
        finally
        {
            await DisposeTransactions(transactions).ConfigureAwait(false);
        }
    }

    private static async Task<IDictionary<string, object?>> ProcessWriteOperation(JsonNode? parsedBody, Dictionary<string, ITransaction> transactions, WriteOperation writeOperation, IDictionary<string, object?> parameterValues)
    {
        var transaction = transactions[writeOperation.ConnectionName];

        var parametersForWriteOperation = new Dictionary<string, object?>(parameterValues);

        MergeBodyParameters(parsedBody, writeOperation, parametersForWriteOperation);

        if (writeOperation.OutputCaptures.Any())
            return await transaction.ExecuteQueryAsync(writeOperation.Sql, parametersForWriteOperation).ConfigureAwait(false);

        await transaction.ExecuteNonQueryAsync(writeOperation.Sql, parametersForWriteOperation).ConfigureAwait(false);
        return new Dictionary<string, object?>();
    }

    private static void MergeBodyParameters(JsonNode? parsedBody, WriteOperation writeOperation, Dictionary<string, object?> parametersForWriteOperation)
    {
        if (writeOperation.BodyType == WriteOperationBodyType.Raw)
        {
            if (writeOperation.RawBodyParameterName is null)
                throw new InvalidOperationException("Cannot use body as parameter value if no BodyParameterName is provided");

            if (!parametersForWriteOperation.TryAdd(writeOperation.RawBodyParameterName, parsedBody?.GetValue<object?>()))
                throw new InvalidOperationException($"Cannot add body parameter '{writeOperation.RawBodyParameterName}' to parameters for write operation. A parameter with the same name already exists.");
        }
        else if (writeOperation.BodyType == WriteOperationBodyType.Object)
        {
            if (parsedBody is not JsonObject bodyAsJsonObject)
                throw new InvalidOperationException("Cannot use body as parameter value if body is not a JSON object");

            foreach (var kvp in bodyAsJsonObject)
                if (!parametersForWriteOperation.TryAdd(kvp.Key, kvp.Value?.GetValue<object?>()))
                    throw new InvalidOperationException($"Cannot add body parameter '{kvp.Key}' to parameters for write operation. A parameter with the same name already exists.");
        }
    }

    private async Task CommitTransactions(Dictionary<string, ITransaction> transactions)
    {
        foreach (var kvp in transactions)
        {
            await kvp.Value.CommitAsync().ConfigureAwait(false);
            logger.LogInformation("Commited transaction {name}", kvp.Key);
        }
    }

    private async Task DisposeTransactions(Dictionary<string, ITransaction> transactions)
    {
        foreach (var kvp in transactions)
        {
            try
            {
                await kvp.Value.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error while disposing transaction {name}", kvp.Key);
            }
        }
    }

    private async Task RollbackTransactions(Dictionary<string, ITransaction> transactions)
    {
        foreach (var kvp in transactions)
        {
            try
            {
                await kvp.Value.RollbackAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error while rolling back transaction {name}", kvp.Key);
            }
        }
    }

    private async Task<Dictionary<string, ITransaction>> BeginTransactions(Endpoint endpoint)
    {
        var transactions = new Dictionary<string, ITransaction>();

        foreach (var connectionName in endpoint.WriteOperations.Select(o => o.ConnectionName).Distinct())
        {
            var transaction = await queryDispatcher.BeginTransactionAsync(connectionName).ConfigureAwait(false);
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
