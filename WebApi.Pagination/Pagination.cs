using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace WebApi.Pagination
{
    /// <summary>
    /// Applies pagination to <see cref="IQueryable{T}"/>s.
    /// </summary>
    public static class Pagination
    {
        /// <summary>
        /// Applies pagination to a queryable data source.
        /// </summary>
        /// <param name="source">The data source to get elements from.</param>
        /// <param name="request">The request message to check for pagination requests to apply to the <paramref name="source"/>.</param>
        /// <param name="firstIndex">Returns the index of the first element selected by the range request.</param>
        /// <exception cref="InvalidOperationException"><paramref name="request"/> contains no pagination requests.</exception>
        /// <exception cref="ArgumentException"><paramref name="request"/> specifies neither <see cref="RangeItemHeaderValue.From"/> nor <see cref="RangeItemHeaderValue.To"/>.</exception>
        public static IQueryable<T> Paginate<T>(this IQueryable<T> source, HttpRequestMessage request, out long firstIndex)
        {
            return source.Paginate(request.Headers.Range.Ranges.First(), out firstIndex);
        }

        /// <summary>
        /// Applies pagination to a queryable data source.
        /// </summary>
        /// <param name="source">The data source to get elements from.</param>
        /// <param name="range">The pagination request to apply to the <paramref name="source"/>.</param>
        /// <param name="firstIndex">Returns the index of the first element selected by the range request.</param>
        /// <exception cref="ArgumentException"><paramref name="range"/> specifies neither <see cref="RangeItemHeaderValue.From"/> nor <see cref="RangeItemHeaderValue.To"/>.</exception>
        public static IQueryable<T> Paginate<T>(this IQueryable<T> source, RangeItemHeaderValue range, out long firstIndex)
        {
            if (range.From.HasValue)
            {
                firstIndex = range.From.Value;
                if (range.To.HasValue) return source.Subset(range.From.Value, range.To.Value);
                else return source.Skip(range.From.Value);
            }
            else if (range.To.HasValue)
                return source.Tail(range.To.Value, out firstIndex);
            else
                throw new ArgumentException("Range must specify upper or lower bound or both.");
        }

        /// <summary>
        /// Retrieves a subset of all elements in a queryable data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        /// <param name="from">The index of the first element to retrieve (inclusive).</param>
        /// <param name="to">The index of the last element to retrieve (inclusive).</param>
        private static IQueryable<T> Subset<T>(this IQueryable<T> source, long from, long to)
        {
            return source.Skip((int)from).Take((int)(to - from + 1));
        }

        /// <summary>
        /// Retrieves all elements in a queryable data source skipping a specific number of elements at the start. Performs long polling if the result set is empty.
        /// </summary>
        /// <param name="source">The data source.</param>
        /// <param name="from">The index of the first element to retrieve (inclusive).</param>
        private static IQueryable<T> Skip<T>(this IQueryable<T> source, long from)
        {
            return Queryable.Skip(source, (int)from);
        }

        /// <summary>
        /// Retrieves the last n elements in a queryable data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        /// <param name="tail">The number of elements to retrieve.</param>
        /// <param name="from">Returns the index of the first retrieved element (inclusive).</param>
        private static IQueryable<T> Tail<T>(this IQueryable<T> source, long tail, out long from)
        {
            from = source.LongCount() - tail;
            return source.Subset(from, from + tail);
        }
    }
}