using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using PaginationSample.Models;
using WebApi.Pagination;

namespace PaginationSample.Controllers
{
    /// <summary>
    /// Demonstrates the usage of the <see cref="HttpPagination"/> extension methods.
    /// </summary>
    [RoutePrefix("extension-methods")]
    public class ExtensionMethodsController : ApiController
    {
        // This is a stand-in for a real queryable data source, such as a database
        private static readonly IQueryable<Person> Persons =
            new[] {new Person("John", "Doe"), new Person("Jane", "Smith")}.
                AsQueryable();

        /// <summary>
        /// Normal response with no pagination.
        /// </summary>
        [HttpGet, Route("normal"), ResponseType(typeof(IEnumerable<Person>))]
        public HttpResponseMessage Normal() => Request.CreateResponse(Persons);

        /// <summary>
        /// Response with pagination.
        /// </summary>
        [HttpGet, Route("pagination"), ResponseType(typeof(IEnumerable<Person>))]
        public HttpResponseMessage Pagination() => Request.CreateResponsePagination(Persons);

        /// <summary>
        /// Response with pagination and long-polling for open ended ranges.
        /// </summary>
        [HttpGet, Route("long-polling"), ResponseType(typeof(IEnumerable<Person>))]
        public Task<HttpResponseMessage> LongPolling() => Request.CreateResponsePaginationAsync(Persons, longPolling: true);

        /// <summary>
        /// Response with pagination and long-polling for open ended ranges.
        /// </summary>
        [HttpGet, Route("long-polling-with-end"), ResponseType(typeof(IEnumerable<Person>))]
        public Task<HttpResponseMessage> LongPollingWithEnd() => Request.CreateResponsePaginationAsync(Persons, longPolling: true, endCondition: x => x.LastName == "Smith");
    }
}