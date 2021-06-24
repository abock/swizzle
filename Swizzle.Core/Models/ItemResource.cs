using System;
using System.IO;

using Microsoft.Extensions.FileProviders;

namespace Swizzle.Models
{
    public sealed record ItemResource(
        ItemResourceKind Kind,
        string PhysicalPath,
        long Size,
        DateTimeOffset CreationTime,
        DateTimeOffset LastWriteTime) : IFileInfo
    {
        public bool Exists => File.Exists(PhysicalPath);
        bool IFileInfo.IsDirectory => false;
        DateTimeOffset IFileInfo.LastModified => LastWriteTime;
        long IFileInfo.Length => Size;
        string IFileInfo.Name => Path.GetFileName(PhysicalPath);

        Stream IFileInfo.CreateReadStream()
            => File.OpenRead(PhysicalPath);
    }
}
