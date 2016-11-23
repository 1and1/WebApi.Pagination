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
        /// The default value for how many database queries are performed for a single long polling request before terminating the connection and requiring the client to reconnect.
        /// </summary>
        public const int DefaultQueriesPerRequest = 10;

        /// <summary>
        /// The default value for how many milliseconds to wait between database queries for long polling.
        /// </summary>
        public const int DefaultQueryDelayMs = 1500;

        /// <summary>
        /// Generates a response message from a queryable data source and applies any pagination requests specified in the request message.
        /// </summary>
        /// <param name="request">The request message to check for pagination requests.</param>
        /// <param name="source">The queryable data source to apply pagination and long polling to and return in response message.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        /// <param name="maxCount">The maximum number of elements that clients may retrieve in a single request. <c>0</c> for no limit. Setting this forces consumers to use pagination.</param>
        public static HttpResponseMessage CreateResponsePagination<T>(this HttpRequestMessage request, IQueryable<T> source, string unit = DefaultUnit, long maxCount = 0)
        {
            if (request.GetOriginalMethod() == HttpMethod.Head)
                return request.CreateResponse(HttpStatusCode.OK).Advertise(unit);

            RangeItemHeaderValue range;
            try
            {
                range = GetRange(request, unit, maxCount);
            }
            catch (InvalidOperationException ex)
            {
                return request.CreateErrorResponse(HttpStatusCode.RequestEntityTooLarge, ex.Message).Advertise(unit);
            }

            if (range == null)
                return request.CreateResponse(HttpStatusCode.OK, source.ToList()).Advertise(unit);

            IQueryable<T> paginatedData;
            long firstIndex;
            try
            {
                paginatedData = source.Paginate(range, out firstIndex);
            }
            catch (ArgumentException ex)
            {
                return request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message).Advertise(unit);
            }

            return request.CreateResponsePagination(paginatedData.ToList(), firstIndex, source.LongCount(), unit).Advertise(unit);
        }

        /// <summary>
        /// Generates a response message from a queryable data source and applies any pagination requests specified in the request message.
        /// </summary>
        /// <param name="request">The request message to check for pagination requests.</param>
        /// <param name="source">The queryable data source to apply pagination and long polling to and return in response message.</param>
        /// <param name="longPolling">Controls whether to use long polling for open-ended ranges.</param>
        /// <param name="endCondition">A check to determine whether an entity is the last element in the stream and no further polling is required. Only relevant if <paramref name="longPolling"/> is <c>true</c>.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        /// <param name="maxCount">The maximum number of elements that clients may retrieve in a single request. <c>0</c> for no limit. Setting this forces consumers to use pagination.</param>
        /// <param name="queriesPerRequest">How many database queries are performed for a single long polling request before terminating the connection and requiring the client to reconnect.</param>
        /// <param name="queryDelay">How many milliseconds to wait between database queries for long polling.</param>
        /// <param name="cancellationToken">Used to cancel the polling.</param>
        public static async Task<HttpResponseMessage> CreateResponsePaginationAsync<T>(this HttpRequestMessage request, IQueryable<T> source, bool longPolling, Predicate<T> endCondition = null, string unit = DefaultUnit, long maxCount = 0, int queriesPerRequest = DefaultQueriesPerRequest, int queryDelay = DefaultQueryDelayMs, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request.GetOriginalMethod() == HttpMethod.Head)
                return request.CreateResponse(HttpStatusCode.OK).Advertise(unit);

            RangeItemHeaderValue range;
            try
            {
                range = GetRange(request, unit, maxCount);
            }
            catch (InvalidOperationException ex)
            {
                return request.CreateErrorResponse(HttpStatusCode.RequestEntityTooLarge, ex.Message).Advertise(unit);
            }

            if (range == null)
                return request.CreateResponse(HttpStatusCode.OK, source.ToList()).Advertise(unit);

            IQueryable<T> paginatedData;
            long firstIndex;
            try
            {
                paginatedData = source.Paginate(range, out firstIndex);
            }
            catch (ArgumentException ex)
            {
                return request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message).Advertise(unit);
            }

            return (longPolling && range.IsHalfOpen()
                ? request.CreateResponsePaginationLongPolling(await paginatedData.ToListLongPollAsync(queriesPerRequest, queryDelay, cancellationToken), firstIndex, unit, endCondition ?? (_ => false))
                : request.CreateResponsePagination(paginatedData.ToList(), firstIndex, source.LongCount(), unit)).Advertise(unit);
        }

        /// <summary>
        /// Determines the orginal <see cref="HttpMethod"/> used in the <paramref name="request"/>. This looks past possible replacements performed by other middleware.
        /// </summary>
        private static HttpMethod GetOriginalMethod(this HttpRequestMessage request)
        {
            object originalMethod;
            request.Properties.TryGetValue("OriginalMethod", out originalMethod);
            return originalMethod as HttpMethod ?? request.Method;
        }

        /// <summary>
        /// Determines the range of elements requested.
        /// </summary>
        /// <param name="request">The request message to check for pagination requests.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        /// <param name="maxCount">The maximum number of elements that clients may retrieve in a single request. <c>0</c> for no limit.</param>
        /// <returns>The requested range or <c>null</c> if all elements should be retrieved.</returns>
        /// <exception cref="InvalidOperationException">The request range violates the specified <paramref name="maxCount"/>.</exception>
        private static RangeItemHeaderValue GetRange(HttpRequestMessage request, string unit, long maxCount)
        {
            var range = (request.Headers.Range == null || request.Headers.Range.Unit != unit)
                ? null
                : request.Headers.Range.Ranges.FirstOrDefault();

            if (maxCount != 0)
            {
                if (range == null)
                    throw new InvalidOperationException($"The request is attempting to retrieve all elements. However, a single request may not retrieve more than {maxCount} elements.");

                if (!range.To.HasValue)
                    throw new InvalidOperationException($"The request is attempting to retrieve an open-ended set of elements. However, a single request may not retrieve more than {maxCount} elements.");

                long count = (range.To - range.From + 1) ?? range.To.Value;
                if (count > maxCount)
                    throw new InvalidOperationException($"The request is attempting to retrieve {count} elements. However, a single request may not retrieve more than {maxCount} elements.");
            }

            return range;
        }

        /// <summary>
        /// Determines whether a range represents a half-open interval (e.g. <c>5-</c> or <c>-10</c>).
        /// </summary>
        private static bool IsHalfOpen(this RangeItemHeaderValue value) => (value.From.HasValue && !value.To.HasValue) || (!value.From.HasValue && value.To.HasValue);

        /// <summary>
        /// Creates a response message for a paginated set of elements.
        /// </summary>
        /// <param name="request">The request message to generate the response for.</param>
        /// <param name="elements">The elements to return in response message.</param>
        /// <param name="firstIndex">The index of the first element in <paramref name="elements"/>.</param>
        /// <param name="totalLength">The total length of the original data source that was paginated.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        private static HttpResponseMessage CreateResponsePagination<T>(this HttpRequestMessage request, IReadOnlyCollection<T> elements, long firstIndex, long totalLength, string unit)
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
        /// Creates a response message for a paginated and long polled set of elements.
        /// </summary>
        /// <param name="request">The request message to generate the response for.</param>
        /// <param name="elements">The elements to return in response message.</param>
        /// <param name="firstIndex">The index of the first element in <paramref name="elements"/>.</param>
        /// <param name="unit">The value used for <see cref="RangeHeaderValue.Unit"/>.</param>
        /// <param name="endCondition">A check to determine whether an entity is the last element in the stream and no further polling is required. May be <c>null</c>.</param>
        private static HttpResponseMessage CreateResponsePaginationLongPolling<T>(this HttpRequestMessage request, IReadOnlyCollection<T> elements, long firstIndex, string unit, Predicate<T> endCondition)
        {
            if (elements.Count == 0)
            {
                return request.CreateErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable,
                    "No elements in requested range at this time. Try again later.");
            }

            var response = request.CreateResponse(HttpStatusCode.PartialContent, elements);
            response.Content.Headers.ContentRange = endCondition(elements.Last())
                ? new ContentRangeHeaderValue(from: firstIndex, to: firstIndex + elements.Count - 1, length: firstIndex + elements.Count)
                : new ContentRangeHeaderValue(from: firstIndex, to: firstIndex + elements.Count - 1);
            response.Content.Headers.ContentRange.Unit = unit;
            return response;
        }

        /// <summary>
        /// Adds a header that advertises the pagination feature to clients.
        /// </summary>
        private static HttpResponseMessage Advertise(this HttpResponseMessage response, string unit)
        {
            response.Headers.AcceptRanges.Add(unit);
            return response;
        }
    }
}