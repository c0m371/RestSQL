using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Json.Path;
using RestSQL.Application.Interfaces;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Application;

public class EndpointService(IQueryDispatcher queryDispatcher, IResultAggregator resultAggregator, IRequestBodyParser requestBodyParser) : IEndpointService
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
                ProcessWriteOperation(parsedBody, transactions, writeOperation, parameterValues);
            }
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

    private static async Task<IDictionary<string, object?> ProcessWriteOperation(JsonNode? parsedBody, Dictionary<string, ITransaction> transactions, WriteOperation writeOperation, IDictionary<string, object?> parameterValues)
    {
        IDictionary<string, object?> output = new Dictionary<string, object?>();

        var transaction = transactions[writeOperation.ConnectionName];

        if (writeOperation.UseRawBodyValue)
        {
            if (writeOperation.BodyParameterName is null)
                throw new InvalidOperationException("Cannot use body as parameter value if no BodyParameterName is provided");


            var parametersForWriteOperation = new Dictionary<string, object?>(parameterValues);
            parametersForWriteOperation.Add(writeOperation.BodyParameterName, parsedBody?.GetValue<object?>());

            if (writeOperation.OutputCaptures.Any())
                output = await transaction.ExecuteQueryAsync(writeOperation.Sql, parametersForWriteOperation).ConfigureAwait(false);
            else
                await transaction.ExecuteNonQueryAsync(writeOperation.Sql, parametersForWriteOperation).ConfigureAwait(false);

            //TODO merge with json case
        }
        if (writeOperation.JsonPath is not null)
        {
            if (!JsonPath.TryParse(writeOperation.JsonPath, out var jsonPath))
                throw new JsonException($"Invalid json path: {writeOperation.JsonPath}");

            var json = jsonPath.Evaluate(parsedBody);

            //TODO continue
        }

        return output;
    }

    private static async Task DisposeTransactions(Dictionary<string, ITransaction> transactions)
    {
        foreach (var tx in transactions.Values)
        {
            try { await tx.DisposeAsync().ConfigureAwait(false); }
            catch { } //TODO LOG  }
        }
    }

    private static async Task RollbackTransactions(Dictionary<string, ITransaction> transactions)
    {
        foreach (var tx in transactions.Values)
        {
            try { await tx.RollbackAsync().ConfigureAwait(false); }
            catch { }//TODO LOG }
        }
    }

    private async Task<Dictionary<string, ITransaction>> BeginTransactions(Endpoint endpoint)
    {
        var transactions = new Dictionary<string, ITransaction>();

        foreach (var connectionName in endpoint.WriteOperations.Select(o => o.Value.ConnectionName).Distinct())
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
