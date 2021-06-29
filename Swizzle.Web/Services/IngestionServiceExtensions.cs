using System;
using System.Threading;

using Microsoft.AspNetCore.Http;

using Swizzle.Models;

namespace Swizzle.Services
{
    public static class IngestionServiceExtensions
    {
        public static (ItemCollection Collection, Uri BaseUri) GetCollection(
            this IngestionService ingestionService,
            HttpRequest httpRequest)
            => (
                ingestionService.GetCollectionOrDefault(httpRequest.Host.Host),
                new Uri($"https://{httpRequest.Host}/"));

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
