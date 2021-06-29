using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.AspNetCore.Http;

using Swizzle.Models;

namespace Swizzle.Dto
{
    public static class ModelExtensions
    {
        public static ItemDto ToDto(
            this Item item,
            HttpRequest request)
            => item.ToDto(new Uri($"https://{request.Host}/"));

        public static ItemDto ToDto(
            this Item item,
            Uri baseUri)
            => new(
                item.Slug,
                new Uri(baseUri, item.Slug),
                item.CreationTime,
                item.LastWriteTime,
                item.Resources
                    .Where(r => r.Exists)
                    .Select(r => r.ToDto(new Uri(
                        baseUri,
                        item.Slug + r.Kind.PreferredExtension)))
                    .ToImmutableArray());

        public static ItemResourceDto ToDto(
            this ItemResource resource,
            Uri uri)
            => new(
                resource.Kind.PreferredContentType,
                uri,
                resource.Size,
                resource.CreationTime,
                resource.LastWriteTime,
                resource.Width,
                resource.Height,
                resource.Duration.TotalSeconds);
    }
}
