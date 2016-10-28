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
        /// The default value for how many query attempts are performed for a long poll before giving up.
        /// </summary>
        public const int DefaultMaxAttempts = 10;

        /// <summary>
        /// The default value for how many milliseconds to wait between query attempts for a long poll.
        /// </summary>
        public const int DefaultDelayMs = 1500;

        /// <summary>
        /// Generates a response message from a queryable data source and applies any pagination requests specified in the request message.
        /// </summary>
        /// <param name="request">The request message to check for pagination requests.</param>
        /// <param name="source">The queryable data source to apply pagination and long polling to and return in response message.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        /// <param name="maxAttempts">How many query attempts are performed for a long poll before giving up.</param>
        /// <param name="delayMs">How many milliseconds to wait between query attempts for a long poll.</param>
        /// <param name="maxCount">The maximum number of elements that clients may retrieve in a single request. <c>0</c> for no limit.</param>
        /// <param name="endCondition">A check to determine whether an entity is the last element in the stream and no further polling is required. May be <c>null</c>.</param>
        public static HttpResponseMessage CreateResponsePagination<T>(this HttpRequestMessage request, IQueryable<T> source, string unit = DefaultUnit, int maxAttempts = DefaultMaxAttempts, int delayMs = DefaultDelayMs, long maxCount = 0, Predicate<T> endCondition = null)
        {
            if (request.Headers.Range == null || request.Headers.Range.Unit != unit)
                return BuildResponseAdvertised(request, source.ToList(), unit);
            var range = request.Headers.Range.Ranges.First();

            try
            {
                CheckRequestedRange(range, maxCount);
            }
            catch (ArgumentException ex)
            {
                return request.CreateErrorResponse(HttpStatusCode.RequestEntityTooLarge, ex.Message);
            }

            IQueryable<T> paginatedData;
            long firstIndex;
            try
            {
                paginatedData = source.Paginate(range, out firstIndex);
            }
            catch (ArgumentException ex)
            {
                return request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
            }

            return BuildResponsePagination(request, paginatedData.ToList(), firstIndex, source.LongCount(), unit);
        }

        /// <summary>
        /// Generates a response message from a queryable data source and applies any pagination requests specified in the request message.
        /// </summary>
        /// <param name="request">The request message to check for pagination requests.</param>
        /// <param name="source">The queryable data source to apply pagination and long polling to and return in response message.</param>
        /// <param name="longPolling">Controls whether to use long polling for open-ended ranges.</param>
        /// <param name="endCondition">A check to determine whether an entity is the last element in the stream and no further polling is required. Only relevant if <paramref name="longPolling"/> is <c>true</c>.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        /// <param name="maxAttempts">How many query attempts are performed for a long poll before giving up.</param>
        /// <param name="delayMs">How many milliseconds to wait between query attempts for a long poll.</param>
        /// <param name="maxCount">The maximum number of elements that clients may retrieve in a single request. <c>0</c> for no limit.</param>
        /// <param name="cancellationToken">Used to cancel the polling.</param>
        public static async Task<HttpResponseMessage> CreateResponsePaginationAsync<T>(this HttpRequestMessage request, IQueryable<T> source, bool longPolling, Predicate<T> endCondition = null, string unit = DefaultUnit, int maxAttempts = DefaultMaxAttempts, int delayMs = DefaultDelayMs, long maxCount = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request.Headers.Range == null || request.Headers.Range.Unit != unit)
                return BuildResponseAdvertised(request, source.ToList(), unit);
            var range = request.Headers.Range.Ranges.First();

            try
            {
                CheckRequestedRange(range, maxCount);
            }
            catch (ArgumentException ex)
            {
                return request.CreateErrorResponse(HttpStatusCode.RequestEntityTooLarge, ex.Message);
            }

            IQueryable<T> paginatedData;
            long firstIndex;
            try
            {
                paginatedData = source.Paginate(range, out firstIndex);
            }
            catch (ArgumentException ex)
            {
                return request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
            }

            return longPolling && range.From.HasValue && !range.To.HasValue
                ? BuildResponsePaginationLongPolling(request, await paginatedData.ToListLongPollAsync(maxAttempts, delayMs, cancellationToken), firstIndex, unit, endCondition ?? (_ => false))
                : BuildResponsePagination(request, paginatedData.ToList(), firstIndex, source.LongCount(), unit);
        }

        private static void CheckRequestedRange(RangeItemHeaderValue range, long maxCount)
        {
            if (maxCount == 0) return;
            if (!range.To.HasValue)
                throw new ArgumentException($"The request is attempting to retrieve an open-ended set of elements. However, a single request may not retrieve more than {maxCount} elements.");

            long count = (range.To - range.From + 1) ?? range.To.Value;
            if (count > maxCount)
                throw new ArgumentException($"The request is attempting to retrieve {count} elements. However, a single request may not retrieve more than {maxCount} elements.");
        }

        /// <summary>
        /// Builds a response message for a paginated set of elements.
        /// </summary>
        /// <param name="request">The request message to generate the response for.</param>
        /// <param name="elements">The elements to return in response message.</param>
        /// <param name="firstIndex">The index of the first element in <paramref name="elements"/>.</param>
        /// <param name="totalLength">The total length of the original data source that was paginated.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        private static HttpResponseMessage BuildResponsePagination<T>(HttpRequestMessage request, IReadOnlyCollection<T> elements, long firstIndex, long totalLength, string unit)
        {
            if (elements.Count == 0)
            {
                return request.CreateErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable,
                    "No elements in requested range.");
            }

            var response = request.CreateResponse(HttpStatusCode.PartialContent, elements);
            response.Headers.AcceptRanges.Add(unit);
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(
                from: firstIndex, to: firstIndex + elements.Count - 1, length: totalLength) {Unit = unit};
            return response;
        }

        /// <summary>
        /// Builds a response message for a paginated and long polled set of elements.
        /// </summary>
        /// <param name="request">The request message to generate the response for.</param>
        /// <param name="elements">The elements to return in response message.</param>
        /// <param name="firstIndex">The index of the first element in <paramref name="elements"/>.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        /// <param name="endCondition">A check to determine whether an entity is the last element in the stream and no further polling is required. May be <c>null</c>.</param>
        private static HttpResponseMessage BuildResponsePaginationLongPolling<T>(HttpRequestMessage request, IReadOnlyCollection<T> elements, long firstIndex, string unit, Predicate<T> endCondition)
        {
            if (elements.Count == 0)
            {
                return request.CreateErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable,
                    "No elements in requested range at this time. Try again later.");
            }

            var response = request.CreateResponse(HttpStatusCode.PartialContent, elements);
            response.Headers.AcceptRanges.Add(unit);
            response.Content.Headers.ContentRange = endCondition(elements.Last())
                ? new ContentRangeHeaderValue(from: firstIndex, to: firstIndex + elements.Count - 1, length: firstIndex + elements.Count)
                : new ContentRangeHeaderValue(from: firstIndex, to: firstIndex + elements.Count - 1);
            response.Content.Headers.ContentRange.Unit = unit;
            return response;
        }

        /// <summary>
        /// Builds a response message for a non-paginated request that advertises the pagination feature to clients.
        /// </summary>
        /// <param name="request">The request message to generate the response for.</param>
        /// <param name="elements">The elements to return in response message.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        private static HttpResponseMessage BuildResponseAdvertised<T>(HttpRequestMessage request, IReadOnlyCollection<T> elements, string unit)
        {
            var response = request.CreateResponse(HttpStatusCode.OK, elements);
            response.Headers.AcceptRanges.Add(unit);
            return response;
        }
    }
}