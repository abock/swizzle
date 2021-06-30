using System;

namespace Swizzle.Dto
{
    public sealed record CreateRedirectionItemDto(
        Uri Target,
        string? Slug = null);
}
