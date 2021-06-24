using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;

using HashidsNet;

using Swizzle.Models;
using Microsoft.Extensions.Logging;

namespace Swizzle.Services
{
    public enum IngestFileOptions
    {
        IngestSingle,
        IngestAlternateResources,
        ProduceAlternateResources
    }

    public sealed class IngestionService
    {
        static readonly Regex s_slugValidator = new(@"^[a-zA-Z0-9]+$");

        public string ContentRootPath { get; }
        public string? DefaultCollectionKey { get; }

        readonly ILogger<IngestionService> _logger;

        ImmutableDictionary<string, ItemCollection> _collections
            = ImmutableDictionary.Create<string, ItemCollection>();

        public ImmutableList<ItemCollection> Collections
            => _collections.Values.ToImmutableList();

        public IngestionService(
            ILogger<IngestionService> logger,
            IOptions<IngestionServiceOptions> options)
            : this(
                logger,
                options.Value.RootDirectory ?? ".")
        {
            DefaultCollectionKey = options.Value.DefaultCollectionKey;
            if (options.Value.CollectionKeys is not null)
            {
                foreach (var collectionKey in options.Value.CollectionKeys)
                    RegisterCollection(collectionKey);
            }
        }

        public IngestionService(
            ILogger<IngestionService> logger,
            string contentRootPath)
        {
            _logger = logger;

            if (contentRootPath is null)
                throw new ArgumentNullException(nameof(contentRootPath));

            ContentRootPath = PathHelpers.ResolveFullPath(contentRootPath);
        }

        public bool TryGetCollection(
            string collectionKey,
            out ItemCollection collection)
            => _collections.TryGetValue(
                collectionKey,
                out collection);

        public ItemCollection GetCollection(string collectionKey)
        {
            if (DefaultCollectionKey is not null &&
                collectionKey is "localhost")
                collectionKey = DefaultCollectionKey;

            if (!TryGetCollection(collectionKey, out var collection))
                throw new CollectionNotRegisteredException(collectionKey);

            return collection;
        }

        static void ValidateSlug(string slug)
        {
            if (!s_slugValidator.IsMatch(slug))
                throw new IllegalSlugException(
                    slug,
                    Hashids.DEFAULT_ALPHABET);
        }

        public void RegisterCollection(string collectionKey)
        {
            if (collectionKey is null)
                throw new ArgumentNullException(nameof(collectionKey));

            if (string.IsNullOrEmpty(collectionKey))
                throw new ArgumentException(
                    "collection must have a valid key (non-null, non-empty)",
                    nameof(collectionKey));

            if (!_collections.ContainsKey(collectionKey))
                _collections = _collections.Add(
                    collectionKey,
                    new ItemCollection(collectionKey));
        }

        public void IngestRegisteredCollections(
            IngestFileOptions options = IngestFileOptions.IngestSingle,
            Func<string, Exception, bool>? itemExceptionHandler = null)
        {
            foreach (var collectionKey in _collections.Keys)
            {
                foreach (var _ in IngestDirectory(
                    Path.Combine(ContentRootPath, collectionKey),
                    options,
                    itemExceptionHandler))
                {
                }

                _logger.LogInformation(
                    "Ingested {Count} items into {CollectionKey}",
                    _collections[collectionKey].Count,
                    collectionKey);
            }
        }

        public IEnumerable<Item> IngestDirectory(
            string directoryPath,
            IngestFileOptions options = IngestFileOptions.IngestSingle,
            Func<string, Exception, bool>? itemExceptionHandler = null)
        {
            _logger.LogInformation(
                "Ingesting directory: {DirectoryPath}",
                directoryPath);

            foreach (var filePath in Directory.EnumerateFiles(directoryPath))
            {
                Item? item = null;

                try
                {
                    item = IngestFile(filePath, options);
                }
                catch (Exception e)
                {
                    if (itemExceptionHandler is not null)
                    {
                        if (!itemExceptionHandler(filePath, e))
                            yield break;
                    }
                }

                if (item is not null)
                    yield return item;
            }
        }

        public Item IngestFile(
            string filePath,
            IngestFileOptions options = IngestFileOptions.IngestSingle)
        {
            var ingestionMetadata = ValidateIngestionPath(filePath);

            if (!ingestionMetadata.FileExists)
                throw new FileNotFoundException(
                    $"file does not exist: {ingestionMetadata.FilePath}",
                    ingestionMetadata.FilePath);

            return IngestFile(ingestionMetadata, options);
        }

        Item IngestFile(
            ValidatedIngestionMetadata ingestionMetadata,
            IngestFileOptions options)
        {
            _logger.LogTrace(
                "Ingesting: {FilePath}",
                ingestionMetadata.FilePath);

            ingestionMetadata.Collection.TryGetItemBySlug(
                ingestionMetadata.Slug,
                out var item);

            var originalItem = item;
            var collectionUpdateNeeded = false;

            if (item is null)
            {
                collectionUpdateNeeded = true;
                item = new Item(
                    Path.Combine(
                        ingestionMetadata.CollectionPath,
                        ingestionMetadata.Slug),
                    ingestionMetadata.CollectionKey,
                    ingestionMetadata.Slug,
                    ingestionMetadata.CreateItemResource());
            }
            else if (!item.TryGetResource(ingestionMetadata.FilePath, out _))
            {
                collectionUpdateNeeded = true;
                item = item.AddResource(
                    ingestionMetadata.CreateItemResource());
            }

            if (collectionUpdateNeeded)
            {
                _collections = _collections.SetItem(
                    ingestionMetadata.CollectionKey,
                    originalItem is null
                        ? ingestionMetadata.Collection.Add(item)
                        : ingestionMetadata.Collection.Replace(
                            originalItem,
                            item));
            }

            if (options != IngestFileOptions.IngestSingle)
            {
                item = IngestAlternateResources(
                    item,
                    produceResources: false);

                if (options == IngestFileOptions.ProduceAlternateResources)
                    item = IngestAlternateResources(
                        item,
                        produceResources: true);
            }

            return item;
        }

        Item IngestAlternateResources(Item item, bool produceResources)
        {
            foreach (var kind in ItemResourceKind.All)
            {
                if (!item.TryGetResource(kind, out _))
                {
                    foreach (var extension in kind.Extensions)
                    {
                        var filePath = item.BasePath + extension;
                        var ingestionMetadata = ValidateIngestionPath(filePath);

                        if (!ingestionMetadata.FileExists &&
                            produceResources &&
                            extension == kind.PreferredExtension &&
                            ProduceResource(item, ingestionMetadata))
                            ingestionMetadata = ValidateIngestionPath(filePath);

                        if (ingestionMetadata.FileExists)
                            item = IngestFile(
                                ingestionMetadata,
                                IngestFileOptions.IngestSingle);
                    }
                }
            }

            return item;
        }

        static bool ProduceResource(
            Item item,
            ValidatedIngestionMetadata ingestionMetadata)
        {
            foreach (var existingResource in item.Resources)
            {
                try
                {
                    if (ItemResourceProductionService.ConvertItemResource(
                        existingResource.Kind,
                        existingResource.PhysicalPath,
                        ingestionMetadata.ResourceKind,
                        ingestionMetadata.FilePath))
                        return true;
                }
                catch
                {
                }
            }

            return false;
        }

        readonly struct ValidatedIngestionMetadata
        {
            public ItemResourceKind ResourceKind { get; init; }
            public string CollectionKey { get; init; }
            public string CollectionPath { get; init; }
            public ItemCollection Collection { get; init; }
            public string FilePath { get; init; }
            public bool FileExists { get; init; }
            public string Slug { get; init; }

            public ItemResource CreateItemResource()
            {
                var fileInfo = new FileInfo(FilePath);
                return new ItemResource(
                    ResourceKind,
                    FilePath,
                    fileInfo.Length,
                    fileInfo.CreationTime,
                    fileInfo.LastWriteTime);
            }
        }

        ValidatedIngestionMetadata ValidateIngestionPath(string filePath)
        {
            static string GetDirectoryName(string path)
            {
                var dir = Path.GetDirectoryName(path);
                return string.IsNullOrEmpty(dir) ? "." : dir;
            }

            if (filePath is null)
                throw new ArgumentNullException(nameof(filePath));

            filePath = PathHelpers.NormalizePath(filePath);

            var fileName = Path.GetFileName(filePath);
            var slug = Path.GetFileNameWithoutExtension(filePath);
            var collectionPath = PathHelpers.ResolveFullPath(
                GetDirectoryName(filePath));
            var contentRootPath = PathHelpers.ResolveFullPath(
                GetDirectoryName(collectionPath));

            ValidateSlug(slug);

            if (contentRootPath != ContentRootPath)
                throw new IllegalPathException(
                    $"refusing to ingest file '{filePath}' as it does " +
                    $"not live inside the content root '{ContentRootPath}'");

            var collectionKey = Path.GetFileName(collectionPath);

            if (!_collections.TryGetValue(collectionKey, out var collection))
                throw new CollectionNotRegisteredException(collectionKey);

            filePath = PathHelpers.ResolveFullPath(
                ContentRootPath,
                collectionKey,
                fileName);

            if (!ItemResourceKind.TryGetFromFileName(
                filePath,
                out var resourceKind))
                throw new UnsupportedContentTypeException(
                    $"unsupported content type: {filePath}");

            return new()
            {
                ResourceKind = resourceKind,
                CollectionPath = collectionPath,
                CollectionKey = collectionKey,
                Collection = collection,
                FilePath = filePath,
                FileExists = File.Exists(filePath),
                Slug = slug
            };
        }
    }
}
