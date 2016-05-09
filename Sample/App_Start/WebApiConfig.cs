using System.Web.Http;

namespace PaginationSample
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.EnsureInitialized();
        }
    }
}