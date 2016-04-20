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

        public long? MaxCount { get; set; }

        public PaginationAttribute()
        {
            Unit = HttpPagination.DefaultUnit;
            MaxCount = null;
        }

        public PaginationAttribute(long maxCount)
        {
            Unit = HttpPagination.DefaultUnit;
            MaxCount = maxCount;
        }

        protected override Task<HttpResponseMessage> BuildResponseMessageAsync<T>(HttpRequestMessage request,
            IQueryable<T> content, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.CreateResponsePagination(content, Unit));
        }
    }
}