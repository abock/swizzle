using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Swizzle.Models
{
    public sealed class Item
    {
        public string BasePath { get; }
        public string CollectionKey { get; }
        public string Slug { get; }
        readonly ImmutableList<ItemResource> _resources;
        public IReadOnlyList<ItemResource> Resources => _resources;
        public int Generation { get; }

        public DateTimeOffset CreationTime => Resources.Count == 0
            ? DateTimeOffset.MinValue
            : Resources.Min(r => r.CreationTime);

        public DateTimeOffset LastWriteTime => Resources.Count == 0
            ? DateTimeOffset.MinValue
            : Resources.Max(r => r.CreationTime);

        public bool Exists => Resources.Any(r => r.Exists);

        Item(
            string basePath,
            string collectionKey,
            string slug,
            ImmutableList<ItemResource> resources,
            int generation)
        {
            BasePath = basePath;
            CollectionKey = collectionKey;
            Slug = slug;
            _resources = resources;
            Generation = generation;
        }

        public Item(
            string basePath,
            string collectionKey,
            string slug,
            ItemResource resource)
            : this(
                basePath,
                collectionKey,
                slug,
                ImmutableList.Create(resource),
                0)
        {
        }

        public Item AddResource(ItemResource resource)
            => new(
                BasePath,
                CollectionKey,
                Slug,
                _resources.Add(resource),
                Generation + 1);

        public Item RemoveResource(ItemResource resource)
            => new(
                BasePath,
                CollectionKey,
                Slug,
                _resources.Remove(resource),
                Generation + 1);

        public bool TryGetResource(
            string filePath,
            out ItemResource resource)
        {
            #nullable disable
            resource = _resources.Find(r => r.PhysicalPath == filePath);
            #nullable restore
            return resource is not null;
        }

        public bool TryGetResource(
            ItemResourceKind kind,
            out ItemResource resource)
        {
            #nullable disable
            resource = _resources.Find(r => r.Kind == kind);
            #nullable restore
            return resource is not null;
        }

        public ItemResource? FindResource(ItemResourceKind kind)
            => TryGetResource(kind, out var resource)
                ? resource
                : null;

        public ItemResource? FindResourceByContentType(string contentType)
            => FindResource(ItemResourceKind.FromContentType(contentType));
    }
}
