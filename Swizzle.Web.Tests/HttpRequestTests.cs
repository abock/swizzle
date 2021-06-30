using System;
using System.Linq;

using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Primitives;

using Xunit;

using Swizzle.Services;

namespace Swizzle.Web.Tests
{
    public class HttpRequestTests : IngestionServiceTestBase
    {
        static HttpRequest CreateRequest(
            string url,
            params (string Header, string Value)[] headers)
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            
            var uri = new Uri(url);
            request.Scheme = uri.Scheme;
            request.IsHttps = string.Equals(
                request.Scheme,
                "https",
                StringComparison.OrdinalIgnoreCase);
            request.Host = new(uri.Host, uri.Port);
            request.Path = uri.AbsolutePath;
            request.QueryString = new(uri.Query);

            foreach (var (header, value) in headers)
                request.Headers[header] = StringValues.Concat(
                    request.Headers[header],
                    value);

            return request;
        }

        [Theory]
        [InlineData(
            "collection1",
            "https://localhost:5001/",
            "https://localhost:5001")]
        [InlineData(
            "collection2",
            "http://localhost:5001/?X-Swizzle-Collection-Key=collection2",
            "http://localhost:5001",
            "x-swizzle-collection-key: collection2")]
        [InlineData(
            "collection3",
            "http://localhost:5001/?X-Swizzle-Collection-Key=collection3",
            "http://localhost:5001",
            "x-swizzle-collection-key: collection2",
            "X-SWIZZLE-COLLECTION-KEY: collection3")]
        [InlineData(
            "collection4",
            "http://localhost:5001/?X-Swizzle-Collection-Key=collection4",
            "http://localhost:5001?a=1&b=2&x-swizzle-collection-KEY=collection4",
            "X-SWIZZLE-COLLECTION-KEY: collection3")]
        [InlineData(
            "collection2",
            "http://localhost:5001/?X-Swizzle-Collection-Key=collection2",
            "http://localhost:5001?a=1&b=2&x-swizzle-collection-KEY=collection4&X-Swizzle-Collection-Key=collection2",
            "X-SWIZZLE-COLLECTION-KEY: collection3")]
        [InlineData(
            "collection4",
            "https://collection4:5001/",
            "https://collection4:5001")]
        [InlineData(
            "collection4",
            "https://collection4:5001/",
            "http://collection4:5001",
            "X-Forwarded-Proto: https")]
        public void Test1(
            string expectedCollectionKey,
            string expectedBaseUrl,
            string url,
            params string[] headers)
        {
            var service = CreateService(
                "collection1",
                new[]
                {
                    "collection1",
                    "collection2",
                    "collection3",
                    "collection4"
                });

            var request = CreateRequest(
                url,
                headers.Select(header =>
                {
                    var parts = header.Split(':', 2);
                    return (parts[0].Trim(), parts[1].Trim());
                }).ToArray());

            Assert.Equal(
                expectedCollectionKey,
                service.GetCollection(request, out var baseUri).Key);
            
            Assert.Equal(expectedBaseUrl, baseUri.OriginalString);
        }
    }
}
