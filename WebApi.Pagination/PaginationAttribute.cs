using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Pagination
{
    /// <summary>
    /// Apply any pagination requests specified in HTTP requests to <see cref="IQueryable{T}"/> responses.
    /// </summary>
    public class PaginationAttribute : QueryableResponseFilterAttribute
    {
        /// <summary>
        /// Controls whether to use long polling for open-ended ranges.
        /// </summary>
        public bool LongPolling { get; set; }

        /// <summary>
        /// The value used for <see cref="RangeHeaderValue.Unit"/>.
        /// </summary>
        public string Unit { get; set; } = HttpPagination.DefaultUnit;

        /// <summary>
        /// The maximum number of elements that clients may retrieve in a single request. <c>0</c> for no limit.
        /// </summary>
        public long MaxCount { get; set; }

        /// <summary>
        /// How many database queries are performed for a single long polling request before terminating the connection and requiring the client to reconnect.
        /// </summary>
        public int QueriesPerRequest { get; set; } = HttpPagination.DefaultQueriesPerRequest;

        /// <summary>
        /// How many milliseconds to wait between database queries for long polling.
        /// </summary>
        public int QueryDelayMs { get; set; } = HttpPagination.DefaultQueryDelayMs;

        protected override Task<HttpResponseMessage> BuildResponseMessageAsync<T>(HttpRequestMessage request, IQueryable<T> content, CancellationToken cancellationToken) =>
            request.CreateResponsePaginationAsync(content, LongPolling, null, Unit, MaxCount, QueriesPerRequest, QueryDelayMs, cancellationToken);
    }
}