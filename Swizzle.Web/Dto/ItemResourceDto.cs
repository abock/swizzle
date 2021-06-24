using System;

namespace Swizzle.Dto
{
    public sealed record ItemResourceDto(
        string ContentType,
        Uri Uri,
        long Size,
        DateTimeOffset CreationTime,
        DateTimeOffset LastWriteTime);
}
