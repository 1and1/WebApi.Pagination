using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace WebApi.Pagination
{
    /// <summary>
    /// Common base class for action filters that replace response messages for <see cref="IQueryable{T}"/> content with their own response messages.
    /// </summary>
    public abstract class QueryableResponseFilterAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext,
            CancellationToken cancellationToken)
        {
            var objectContent = actionExecutedContext.Response.Content as ObjectContent;
            if (objectContent != null)
            {
                actionExecutedContext.Response = await BuildResponseMessageAsync(
                    actionExecutedContext.Request, objectContent.Value, cancellationToken);
            }

            await base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }

        /// <summary>
        /// Builds a response message for a request using the given content.
        /// </summary>
        /// <param name="request">The request to create the response for.</param>
        /// <param name="content">The content to return in response message.</param>
        /// <param name="cancellationToken">Used to cancel building the response.</param>
        private Task<HttpResponseMessage> BuildResponseMessageAsync(HttpRequestMessage request, object content,
            CancellationToken cancellationToken)
        {
            var queryableInterface = GetQueryableInterface(content.GetType());
            var methodInfo = HelperMethodInfo.MakeGenericMethod(queryableInterface.GetGenericArguments());
            return (Task<HttpResponseMessage>)methodInfo.Invoke(this, new[] {request, content, cancellationToken});
        }

        #region Reflection helper
        private static Type GetQueryableInterface(Type type)
        {
            var queryableInterface = type.GetInterfaces()
                .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof (IQueryable<>));
            if (queryableInterface == null)
                throw new InvalidOperationException(type.Name + " does not implement IQueryable<T>.");
            return queryableInterface;
        }

        private static readonly MethodInfo HelperMethodInfo =
            typeof (QueryableResponseFilterAttribute).GetMethod("HelperAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);

        // ReSharper disable once UnusedMember.Local, invoked via reflection
        private Task<HttpResponseMessage> HelperAsync<T>(HttpRequestMessage request,
            IQueryable<T> content, CancellationToken cancellationToken)
        {
            return BuildResponseMessageAsync(request, content, cancellationToken);
        }
        #endregion

        /// <summary>
        /// Builds a response message for a request using the given content.
        /// </summary>
        /// <param name="request">The request to create the response for.</param>
        /// <param name="content">The content to return in response message.</param>
        /// <param name="cancellationToken">Used to cancel building the response.</param>
        protected abstract Task<HttpResponseMessage> BuildResponseMessageAsync<T>(HttpRequestMessage request,
            IQueryable<T> content, CancellationToken cancellationToken);
    }
}