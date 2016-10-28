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
        /// The value used for <see cref="RangeHeaderValue.Unit"/>.
        /// </summary>
        public string Unit { get; set; } = HttpPagination.DefaultUnit;

        /// <summary>
        /// Controls whether to use long polling for open-ended ranges.
        /// </summary>
        public bool LongPolling { get; set; }

        /// <summary>
        /// How many database query attempts are performed for a single long poll before terminating the connection and requiring the client to reconnect.
        /// </summary>
        public int MaxAttempts { get; set; } = HttpPagination.DefaultMaxAttempts;

        /// <summary>
        /// How many milliseconds to wait between query attempts for a long poll.
        /// </summary>
        public int DelayMs { get; set; } = HttpPagination.DefaultDelayMs;

        /// <summary>
        /// The maximum number of elements that clients may retrieve in a single request. <c>0</c> for no limit.
        /// </summary>
        public long MaxCount { get; set; }

        protected override Task<HttpResponseMessage> BuildResponseMessageAsync<T>(HttpRequestMessage request, IQueryable<T> content, CancellationToken cancellationToken) =>
            request.CreateResponsePaginationAsync(content, LongPolling, null, Unit, MaxAttempts, DelayMs, MaxCount, cancellationToken);
    }
}