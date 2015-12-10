using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Pagination
{
    /// <summary>
    /// Helpers for building paginated <see cref="HttpResponseMessage"/> from <see cref="HttpRequestMessage"/>s and <see cref="IQueryable{T}"/>s.
    /// </summary>
    public static class HttpPagination
    {
        /// <summary>
        /// The default value used for <see cref="RangeHeaderValue.Unit"/>.
        /// </summary>
        public const string DefaultUnit = "elements";

        /// <summary>
        /// Generates a response message from a queryable data source and applies any pagination requests specified in the request message.
        /// </summary>
        /// <param name="request">The request message to check for pagination requests.</param>
        /// <param name="source">The queryable data source to apply pagination to and return in response message.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        public static HttpResponseMessage CreateResponsePagination<T>(this HttpRequestMessage request,
            IQueryable<T> source, string unit = DefaultUnit)
        {
            if (request.Headers.Range == null || request.Headers.Range.Unit != unit)
                return request.CreateResponseAdvertised(source.ToList(), unit);
            var range = request.Headers.Range.Ranges.First();

            IQueryable<T> paginatedData;
            long firstIndex;
            try
            {
                paginatedData = source.Paginate(range, out firstIndex);
            }
            catch (ArgumentException ex)
            {
                return request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }

            var elements = paginatedData.ToList();
            long totalLength = source.LongCount();
            return request.CreateResponsePagination(elements, firstIndex, totalLength, unit);
        }

        /// <summary>
        /// The default value for how many query attempts are performed for a long poll before giving up.
        /// </summary>
        public const int DefaultMaxAttempts = 10;

        /// <summary>
        /// The default value for how many milliseconds to wait between query attempts for a long poll.
        /// </summary>
        public const int DefaultDelayMs = 1500;

        /// <summary>
        /// Generates a response message from a queryable data source and applies any pagination requests specified in the request message.
        /// Uses long polling for open ended ranges that initally return empty result sets.
        /// </summary>
        /// <param name="request">The request message to check for pagination requests.</param>
        /// <param name="source">The queryable data source to apply pagination and long polling to and return in response message.</param>
        /// <param name="maxAttempts">How many query attempts are performed for a long poll before giving up.</param>
        /// <param name="delayMs">How many milliseconds to wait between query attempts for a long poll.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        public static HttpResponseMessage CreateResponsePaginationLongPolling<T>(this HttpRequestMessage request,
            IQueryable<T> source, int maxAttempts = DefaultMaxAttempts, int delayMs = DefaultDelayMs,
            string unit = DefaultUnit)
        {
            if (request.Headers.Range == null || request.Headers.Range.Unit != unit)
                return request.CreateResponseAdvertised(source.ToList(), unit);
            var range = request.Headers.Range.Ranges.First();

            IQueryable<T> paginatedData;
            long firstIndex;
            try
            {
                paginatedData = source.Paginate(range, out firstIndex);
            }
            catch (ArgumentException ex)
            {
                return request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }

            bool openEndedRange = range.From.HasValue && !range.To.HasValue;
            var elements = openEndedRange
                ? paginatedData.ToListLongPoll(maxAttempts, delayMs)
                : paginatedData.ToList();
            return CreateResponsePaginationLongPolling(request, elements, firstIndex, unit);
        }

        /// <summary>
        /// Generates a response message from a queryable data source and applies any pagination requests specified in the request message.
        /// Uses long polling for open ended ranges that initally return empty result sets.
        /// </summary>
        /// <param name="request">The request message to check for pagination requests.</param>
        /// <param name="source">The queryable data source to apply pagination and long polling to and return in response message.</param>
        /// <param name="maxAttempts">How many query attempts are performed for a long poll before giving up.</param>
        /// <param name="delayMs">How many milliseconds to wait between query attempts for a long poll.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        /// <param name="cancellationToken">Used to cancel the polling.</param>
        public static async Task<HttpResponseMessage> CreateResponsePaginationLongPollingAsync<T>(
            this HttpRequestMessage request,
            IQueryable<T> source, int maxAttempts = DefaultMaxAttempts, int delayMs = DefaultDelayMs,
            string unit = DefaultUnit,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request.Headers.Range == null || request.Headers.Range.Unit != unit)
                return request.CreateResponseAdvertised(source.ToList(), unit);
            var range = request.Headers.Range.Ranges.First();

            IQueryable<T> paginatedData;
            long firstIndex;
            try
            {
                paginatedData = source.Paginate(range, out firstIndex);
            }
            catch (ArgumentException ex)
            {
                return request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }

            bool openEndedRange = range.From.HasValue && !range.To.HasValue;
            var elements = openEndedRange
                ? await paginatedData.ToListLongPollAsync(maxAttempts, delayMs, cancellationToken)
                : paginatedData.ToList();
            return CreateResponsePaginationLongPolling(request, elements, firstIndex, unit);
        }

        /// <summary>
        /// Generates a response message for a paginated set of elements.
        /// </summary>
        /// <param name="request">The request message to generate the response for.</param>
        /// <param name="elements">The elements to return in response message.</param>
        /// <param name="firstIndex">The index of the first element in <paramref name="elements"/>.</param>
        /// <param name="totalLength">The total length of the original data source that was paginated.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        private static HttpResponseMessage CreateResponsePagination<T>(this HttpRequestMessage request,
            IReadOnlyCollection<T> elements, long firstIndex, long totalLength, string unit)
        {
            if (elements.Count == 0)
            {
                return request.CreateErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable,
                    "No elements in requested range.");
            }

            var response = request.CreateResponse(HttpStatusCode.PartialContent, elements);
            response.Headers.AcceptRanges.Add(unit);
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(
                firstIndex, firstIndex + elements.Count - 1, totalLength) {Unit = unit};
            return response;
        }

        /// <summary>
        /// Generates a response message for a paginated and long polled set of elements.
        /// </summary>
        /// <param name="request">The request message to generate the response for.</param>
        /// <param name="elements">The elements to return in response message.</param>
        /// <param name="firstIndex">The index of the first element in <paramref name="elements"/>.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        private static HttpResponseMessage CreateResponsePaginationLongPolling<T>(this HttpRequestMessage request,
            IReadOnlyCollection<T> elements, long firstIndex, string unit)
        {
            if (elements.Count == 0)
            {
                return request.CreateErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable,
                    "No elements in requested range at this time. Try again later.");
            }

            var response = request.CreateResponse(HttpStatusCode.PartialContent, elements);
            response.Headers.AcceptRanges.Add(unit);
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(
                firstIndex, firstIndex + elements.Count - 1) {Unit = unit};
            return response;
        }

        /// <summary>
        /// Generates a response message for a non-paginated request that advertises the pagination feature to clients.
        /// </summary>
        /// <param name="request">The request message to generate the response for.</param>
        /// <param name="elements">The elements to return in response message.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        private static HttpResponseMessage CreateResponseAdvertised<T>(this HttpRequestMessage request,
            IReadOnlyCollection<T> elements, string unit)
        {
            var response = request.CreateResponse(HttpStatusCode.OK, elements);
            response.Headers.AcceptRanges.Add(unit);
            return response;
        }
    }
}