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
        public string Unit { get; set; }

        /// <summary>
        /// The maximum number of elements that clients may retrieve in a single request. <c>0</c> for no limit.
        /// </summary>
        public long MaxCount { get; set; }

        public PaginationAttribute()
        {
            Unit = HttpPagination.DefaultUnit;
        }

        protected override Task<HttpResponseMessage> BuildResponseMessageAsync<T>(HttpRequestMessage request,
            IQueryable<T> content, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.CreateResponsePagination(content, Unit, MaxCount));
        }
    }
}