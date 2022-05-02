using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Nest;

namespace NestClientFactory
{
    public static class ElasticClientExtensions
    {
        public static async Task<TQuery> Find<TQuery>(this IElasticClient client, TQuery query, CancellationToken cancellationToken = default(CancellationToken)) where TQuery : IAuxilioQuery
        {
            await DoFind(client, new IAuxilioQuery[] { query }, cancellationToken);
            return query;
        }

        public static async Task Find(this IElasticClient client, params IAuxilioQuery[] queries)
        {
            await DoFind(client, queries, CancellationToken.None);
        }

        public static async Task Find(this IElasticClient client, CancellationToken cancellationToken = default(CancellationToken), params IAuxilioQuery[] queries)
        {
            await DoFind(client, queries, cancellationToken);
        }

        private static async Task DoFind(IElasticClient client, IAuxilioQuery[] queries, CancellationToken cancellationToken)
        {
            var multiSearchDescriptor = new MultiSearchDescriptor();

            foreach (var part in queries)
                part.Build(multiSearchDescriptor);

            var multiSearchResponse = await client.MultiSearchAsync(multiSearchDescriptor, cancellationToken);

            if (multiSearchResponse.ServerError != null)
                throw new QueryException(multiSearchResponse.ServerError.ToString(), multiSearchResponse.OriginalException);

            foreach (var part in queries)
                part.Extract(multiSearchResponse);
        }
    }

    public class QueryException : Exception
    {
        public QueryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
