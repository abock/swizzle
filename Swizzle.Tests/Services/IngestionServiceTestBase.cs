using System;
using System.IO;

using Microsoft.Extensions.Logging;

namespace Swizzle.Services
{
    public abstract class IngestionServiceTestBase : IDisposable
    {
        protected static string ContentRootPath { get; } = Path.GetFullPath(
            Path.Combine(
                "..", "..", "..", "..",
                "Swizzle.Tests",
                "test-content-root"));

        protected static IngestionService CreateService(
            string? defaultCollectionKey = null,
            string?[]? collectionKeys = null)
            => new(
                LoggerFactory
                    .Create(builder => builder.AddConsole())
                    .CreateLogger<IngestionService>(),
                new IngestionServiceOptions
                {
                    RootDirectory = ContentRootPath,
                    DefaultCollectionKey = defaultCollectionKey,
                    CollectionKeys = collectionKeys
                });

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            foreach (var mutableDir in Directory.EnumerateDirectories(
                ContentRootPath))
            {
                if (Path.GetFileName(mutableDir)[0] == '_')
                    Directory.Delete(mutableDir, recursive: true);
            }
        }
    }
}
