using System;
using System.Threading;

using Microsoft.AspNetCore.Http;

using Swizzle.Models;

namespace Swizzle.Services
{
    public static class IngestionServiceExtensions
    {
        public static ItemCollection GetCollection(
            this IngestionService ingestionService,
            HttpRequest httpRequest)
            => ingestionService.GetCollection(httpRequest, out _);

        public static ItemCollection GetCollection(
            this IngestionService ingestionService,
            HttpRequest httpRequest,
            out Uri baseUri)
        {
            var metadata = httpRequest.GetCollectionRequestMetadata();

            baseUri = metadata.BaseUri;

            if (metadata.AllowDefaultCollection)
                return ingestionService.GetCollectionOrDefault(
                    metadata.CollectionKey);

            return ingestionService.GetCollection(
                metadata.CollectionKey);
        }

        public static Item CreateAndIngestFile(
            this IngestionService ingestionService,
            HttpRequest httpRequest,
            ItemResourceKind resourceKind,
            ReadOnlySpan<byte> resourceData,
            string? slug = null,
            bool replaceResource = false,
            IngestFileOptions options = IngestFileOptions.IngestSingle,
            CancellationToken cancellationToken = default)
            => ingestionService.CreateAndIngestFile(
                ingestionService
                    .GetCollectionOrDefault(httpRequest.Host.Host)
                    .Key,
                resourceKind,
                resourceData,
                slug,
                replaceResource,
                options,
                cancellationToken);
    }
}
