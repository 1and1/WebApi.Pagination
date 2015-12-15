# WebApi.Pagination

WebApi.Pagination allows you to easily add [Range Header-based pagination](http://otac0n.com/blog/2012/11/21/range-header-i-choose-you.html) to existing [WebAPI](http://www.asp.net/web-api) endpoints that operate on `IQueryable` data sources.

NuGet package:
* [WebApi.Pagination](https://www.nuget.org/packages/WebApi.Pagination/)


## Getting started

1. Add the NuGet package to your project.
2. Choose one:
  * Add the `[Pagination]` attribute to endpoint methods.
  * Use the `.CreateResponsePagination()` extension method for `HttpResponseMessage` to build a paginated response from an `IQueryable` source.


## Sample project

The source code includes a sample project that uses demonstrates the usage of WebApi.Pagination. You can build and run it using Visual Studio 2015. By default the instance will be hosted by IIS Express at `http://localhost:2085/`.
