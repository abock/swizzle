using System;

namespace Swizzle.Services
{
    public sealed class IngestionServiceOptions
    {
        public const string SectionName = "SwizzleIngestion";

        public string? RootDirectory { get; set; }
        public string? DefaultCollectionKey { get; set; }
        public string[] CollectionKeys { get; set; } = Array.Empty<string>();
    }
}
