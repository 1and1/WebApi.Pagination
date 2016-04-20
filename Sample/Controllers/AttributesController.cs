using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using WebApi.Pagination.Sample.Models;

namespace WebApi.Pagination.Sample.Controllers
{
    /// <summary>
    /// Demonstrates the usage of the <see cref="PaginationAttribute"/>.
    /// </summary>
    [RoutePrefix("attributes")]
    public class AttributesController : ApiController
    {
        // This is a stand-in for a real queryable data source, such as a database
        private static readonly IQueryable<Person> Persons =
            new[] {new Person("John", "Doe"), new Person("Jane", "Smith")}.
                AsQueryable();

        /// <summary>
        /// Normal response with no pagination.
        /// </summary>
        [HttpGet, Route("normal")]
        public IEnumerable<Person> Normal()
        {
            return Persons;
        }

        /// <summary>
        /// Response with pagination.
        /// </summary>
        [HttpGet, Route("pagination")]
        [Pagination]
        public IQueryable<Person> Pagination()
        {
            return Persons;
        }

        /// <summary>
        /// Response with pagination with a limited result set size.
        /// </summary>
        [HttpGet, Route("pagination-limited")]
        [Pagination(MaxCount = 1)]
        public IQueryable<Person> PaginationLimited()
        {
            return Persons;
        }

        /// <summary>
        /// Response with pagination and long-polling for open ended ranges.
        /// </summary>
        [HttpGet, Route("long-polling")]
        [PaginationLongPolling]
        public IQueryable<Person> LongPolling()
        {
            return Persons;
        }
    }
}