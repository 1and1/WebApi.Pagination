using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Pagination
{
    /// <summary>
    /// Apply any pagination requests specified in HTTP requests to <see cref="IQueryable{T}"/> responses.
    /// Use long polling for open-ended ranges.
    /// </summary>
    public class PaginationLongPollingAttribute : PaginationAttribute
    {
        /// <summary>
        /// How many database query attempts are performed for a single long poll before terminating the connection and requiring the client to reconnect.
        /// </summary>
        public int MaxAttempts { get; set; }

        /// <summary>
        /// How many milliseconds to wait between query attempts for a long poll.
        /// </summary>
        public int DelayMs { get; set; }

        public PaginationLongPollingAttribute()
        {
            MaxAttempts = HttpPagination.DefaultMaxAttempts;
            DelayMs = HttpPagination.DefaultDelayMs;
        }

        protected override Task<HttpResponseMessage> BuildResponseMessageAsync<T>(HttpRequestMessage request,
            IQueryable<T> source, CancellationToken cancellationToken)
        {
            return request.CreateResponsePaginationLongPollingAsync(source, MaxAttempts, DelayMs, Unit, null,
                cancellationToken);
        }
    }
}