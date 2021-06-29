using System;

using Microsoft.AspNetCore.Http;

using Swizzle.Models;

namespace Swizzle.Dto
{
    public static class AspNetModelExtensions
    {
        public static ItemDto ToDto(
            this Item item,
            HttpRequest request)
            => item.ToDto(new Uri($"https://{request.Host}/"));
    }
}
