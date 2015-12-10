using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Pagination
{
    /// <summary>
    /// Applies long polling to <see cref="IQueryable{T}"/>s.
    /// </summary>
    public static class LongPolling
    {
        /// <summary>
        /// Retrieves all elements in a queryable data source. Retries with long polling if the result set is initally empty.
        /// </summary>
        /// <param name="source">The data source to get elements from.</param>
        /// <param name="maxAttempts">How many query attempts are performed for a long poll before giving up.</param>
        /// <param name="delayMs">How many milliseconds to wait between query attempts for a long poll.</param>
        public static List<T> ToListLongPoll<T>(this IQueryable<T> source,
            int maxAttempts = 10, int delayMs = 1500)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                var result = source.ToList();
                if (result.Count > 0) return result;
                Thread.Sleep(delayMs);
            }
            return new List<T>();
        }

        /// <summary>
        /// Retrieves all elements in a queryable data source. Retries with long polling if the result set is initally empty.
        /// </summary>
        /// <param name="source">The data source to get elements from.</param>
        /// <param name="maxAttempts">How many query attempts are performed for a long poll before giving up.</param>
        /// <param name="delayMs">How many milliseconds to wait between query attempts for a long poll.</param>
        /// <param name="cancellationToken">Used to cancel the polling.</param>
        public static async Task<List<T>> ToListLongPollAsync<T>(this IQueryable<T> source,
            int maxAttempts = 10, int delayMs = 1500, CancellationToken cancellationToken = default(CancellationToken))
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                var result = source.ToList();
                if (result.Count > 0) return result;
                await Task.Delay(delayMs, cancellationToken);
            }
            return new List<T>();
        }
    }
}