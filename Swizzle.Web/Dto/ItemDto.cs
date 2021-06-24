using System;
using System.Collections.Immutable;

namespace Swizzle.Dto
{
    public sealed record ItemDto(
        Uri Uri,
        DateTimeOffset CreationTime,
        DateTimeOffset LastWriteTime,
        ImmutableArray<ItemResourceDto> Resources);
}
