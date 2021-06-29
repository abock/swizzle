using System;
using System.Collections.Immutable;
using System.IO;

namespace Swizzle.Models
{
    public sealed class ItemResourceKind : IEquatable<ItemResourceKind>
    {
        public sealed class UnsupportedException : Exception
        {
            public UnsupportedException(string kind)
                : base($"Unsupported content type or file extension: {kind}")
            {
            }
        }

        public static readonly ItemResourceKind Jpeg = new(
            1,
            ImmutableArray.Create(".jpg", ".jpeg"),
            ImmutableArray.Create("image/jpeg"));

        public static readonly ItemResourceKind Gif = new(
            2,
            ImmutableArray.Create(".gif"),
            ImmutableArray.Create("image/gif"));

        public static readonly ItemResourceKind Mp4 = new(
            3,
            ImmutableArray.Create(".mp4"),
            ImmutableArray.Create("video/mp4"));

        public static readonly ItemResourceKind Ogv = new(
            4,
            ImmutableArray.Create(".ogv"),
            ImmutableArray.Create("video/ogg"));

        public static readonly ItemResourceKind Uri = new(
            5,
            ImmutableArray.Create(".uri"),
            ImmutableArray.Create("text/uri-list"));

        // NB. The order is not significant, but tests depend on it
        public static readonly ImmutableArray<ItemResourceKind> All
            = ImmutableArray.Create(Jpeg, Gif, Mp4, Ogv, Uri);

        readonly int _tag;
        public ImmutableArray<string> Extensions { get; }
        public ImmutableArray<string> ContentTypes { get; }
        public string PreferredExtension => Extensions[0];
        public string PreferredContentType => ContentTypes[0];

        ItemResourceKind(
            int tag,
            ImmutableArray<string> extensions,
            ImmutableArray<string> contentTypes)
        {
            _tag = tag;
            Extensions = extensions;
            ContentTypes = contentTypes;
        }

        public override int GetHashCode()
            => _tag;

        public override bool Equals(object? obj)
            => obj is ItemResourceKind other && Equals(other);

        public bool Equals(ItemResourceKind? other)
            => other is not null && other._tag == _tag;

        public static bool operator ==(ItemResourceKind a, ItemResourceKind b)
            => a.Equals(b);

        public static bool operator !=(ItemResourceKind a, ItemResourceKind b)
            => !a.Equals(b);

        public static ItemResourceKind FromContentType(string contentType)
        {
            if (TryGetFromContentType(contentType, out var resourceKind))
                return resourceKind;
            throw new UnsupportedException(contentType);
        }

        public static bool TryGetFromContentType(
            string contentType,
            out ItemResourceKind resourceKind)
        {
            foreach (var kind in All)
            {
                foreach (var kindContentType in kind.ContentTypes)
                {
                    if (string.Equals(
                        kindContentType,
                        contentType,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        resourceKind = kind;
                        return true;
                    }
                }
            }

            #nullable disable
            resourceKind = null;
            #nullable restore
            return false;
        }

        public static ItemResourceKind FromExtension(string extension)
        {
            if (TryGetFromExtension(extension, out var resourceKind))
                return resourceKind;
            throw new UnsupportedException(extension);
        }

        public static bool TryGetFromExtension(
            string extension,
            out ItemResourceKind resourceKind)
        {
            if (extension.Length > 0 && extension[0] != '.')
                extension = "." + extension;

            foreach (var kind in All)
            {
                foreach (var kindExtension in kind.Extensions)
                {
                    if (extension == kindExtension)
                    {
                        resourceKind = kind;
                        return true;
                    }
                }
            }

            #nullable disable
            resourceKind = null;
            #nullable restore
            return false;
        }

        public static ItemResourceKind FromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (TryGetFromExtension(extension, out var resourceKind))
                return resourceKind;
            throw new UnsupportedException(extension);
        }

        public static bool TryGetFromFileName(
            string fileName,
            out ItemResourceKind resourceKind)
            => TryGetFromExtension(
                Path.GetExtension(fileName),
                out resourceKind);
    }
}
