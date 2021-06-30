using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Http;

namespace Swizzle.Services
{
    internal static partial class HttpExtensions
    {
        public static CollectionRequestMetadata GetCollectionRequestMetadata(
            this HttpContext httpContext)
        {
            if (httpContext.Items.TryGetValue(
                typeof(CollectionRequestMetadata),
                out var o) && o is CollectionRequestMetadata metadata)
                return metadata;

            metadata = CollectionRequestMetadata.FromHttpRequest(
                httpContext.Request);

            httpContext.Items[typeof(CollectionRequestMetadata)] = metadata;

            return metadata;
        }

        public static CollectionRequestMetadata GetCollectionRequestMetadata(
            this HttpRequest httpRequest)
            => httpRequest.HttpContext.GetCollectionRequestMetadata();
    }

    internal sealed record CollectionRequestMetadata(
        Uri BaseUri,
        string CollectionKey,
        bool AllowDefaultCollection)
    {
        const string CollectionKeyHeader = "X-Swizzle-Collection-Key";

        // Pull out the collection key in order of precedence (query string
        // overrides headers; headers override host). Within both query
        // string and headers, the last of multiple potential keys will
        // always win (e.g. for ?a=1&a=2, a=2 will be acted upon while
        // a=1 will be ignored entirely).
        static readonly List<Func<HttpRequest, (string? Key, bool AllowDefault)>> s_collectionKeyGetters = new()
        {
            r => (r.Query[CollectionKeyHeader].LastOrDefault(), false),
            r => (r.Headers[CollectionKeyHeader].LastOrDefault(), false),
            r => (r.Host.Host, true)
        };

        public static CollectionRequestMetadata FromHttpRequest(
            HttpRequest httpRequest)
        {
            var (collectionKey, allowDefaultCollectionKey) = s_collectionKeyGetters
                .Select(g => g(httpRequest))
                .Where(c => !string.IsNullOrEmpty(c.Key))
                .FirstOrDefault();

            if (string.IsNullOrEmpty(collectionKey))
                throw new BadHttpRequestException(
                    "HTTP request does not contain a Swizzle collection key");

            var scheme = httpRequest
                .Headers["X-Forwarded-Proto"]
                .LastOrDefault() ?? httpRequest.Scheme;

            if (string.IsNullOrEmpty(scheme))
                scheme = httpRequest.IsHttps ? "https" : "http";

            var baseUriString = $"{scheme}://{httpRequest.Host}/";

            if (!string.Equals(
                httpRequest.Host.Host,
                collectionKey,
                StringComparison.OrdinalIgnoreCase))
                baseUriString += $"?{CollectionKeyHeader}={collectionKey}";

            return new(
                new(baseUriString),
                collectionKey,
                allowDefaultCollectionKey);
        }
    }
}
