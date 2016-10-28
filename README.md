# WebApi.Pagination

WebApi.Pagination allows you to easily add [Range Header-based pagination](http://otac0n.com/blog/2012/11/21/range-header-i-choose-you.html) to existing [WebAPI](http://www.asp.net/web-api) endpoints that operate on `IQueryable` data sources.

NuGet package:
* [WebApi.Pagination](https://www.nuget.org/packages/WebApi.Pagination/)



## Usage

### Attributes

Simply add the `[Pagination]` attribute to a controller method with an `IQueryable<T>` return type.

Sample:
```cs
[Pagination]
public IQueryable<Person> Get()
{
  return Database.Persons.OrderBy(x => x.Name);
}
```

If you add any other filter attributes to your methods make sure the `[Pagination]` attribute is listed last. Otherwise modifications to the response made by other filters will be lost.

Use `[Pagination(LongPolling = true)]` to enable long polling.


### Extension methods

If your controller method returns an `HttpResponseMessage` rather than a message content, you can use the `.CreateResponsePagination()` extension method for `HttpRequestMessage` to build a paginated response.

Sample:
```cs
[Pagination]
public HttpResponseMessage Get(HttpRequestMessage request)
{
  return request.CreateResponsePagination(
    Database.Persons.OrderBy(x => x.Name));
}
```

Use `.CreateResponsePaginationAsync(..., longPolling: true)` to enable long polling.



## Sample project

The source code includes a sample project that uses demonstrates the usage of WebApi.Pagination. You can build and run it using Visual Studio 2015. By default the instance will be hosted by IIS Express at `http://localhost:2085/attributes/` and `http://localhost:2085/extension-methods/`.



## HTTP header samples

### Subset

Request:
```
GET /resource
Range: elements=2-5
```

Response:
```
206 Partial Content
Content-Range: elements 2-3/5

[ 2, 3 ]
```


### Tail

Request:
```
GET /resource
Range: elements=-2
```

Response:
```
206 Partial Content
Content-Range: elements 4-5/5

[ 4, 5 ]
```


### Offset

Request:
```
GET /resource
Range: elements=2-
```

Response:
```
206 Partial Content
Content-Range: elements 2-5/5

[ 2, 3, 4, 5 ]
```


### Long polling

Request:
```
GET /resource
Range: elements=6-
```

Response after timeout with no new content:
```
416 Requested Range
```

Client keeps polling...

Request:
```
GET /resource
Range: elements=6-
```

Response after delay until content became available:
```
206 Partial Content
Content-Range: elements 6-7/*

[ 6, 7 ]
```
