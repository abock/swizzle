using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

using Swizzle.Models;

namespace Swizzle.Formatters
{
    public sealed class ItemResourceOutputFormatter : OutputFormatter
    {
        public ItemResourceOutputFormatter()
        {
            foreach (var kind in ItemResourceKind.All)
            {
                foreach (var contentType in kind.ContentTypes)
                    SupportedMediaTypes.Add(
                        MediaTypeHeaderValue.Parse(contentType));
            }
        }

        protected override bool CanWriteType(Type type)
            => typeof(Item).IsAssignableFrom(type) ||
                typeof(ItemResource).IsAssignableFrom(type);

        public override Task WriteResponseBodyAsync(
            OutputFormatterWriteContext context)
        {
            var resource = context.Object switch
            {
                ItemResource r => r,
                Item item => item.FindResourceByContentType(
                    context.ContentType.Value),
                _ => null
            };

            if (resource is null)
            {
                context.HttpContext.Response.StatusCode = 404;
                return Task.CompletedTask;
            }

            var request = context.HttpContext.Request;
            var response = context.HttpContext.Response;

            var fromByteOffset = 0L;
            var toByteOffset = resource.Size - 1;

            var rangeHeader = request.GetTypedHeaders().Range;

            if (rangeHeader is not null &&
                rangeHeader.Ranges.Count == 1 &&
                rangeHeader.Unit.Equals(
                    "bytes",
                    StringComparison.OrdinalIgnoreCase))
            {
                var range = rangeHeader.Ranges.Single();
                if (range.From.HasValue)
                    fromByteOffset = range.From.Value;
                if (range.To.HasValue)
                    toByteOffset = range.To.Value;
            }

            if (fromByteOffset > 0 || toByteOffset < resource.Size - 1)
            {
                response.StatusCode = 206;
                response.Headers.Add(
                    "Content-Range",
                    $"bytes {fromByteOffset}-{toByteOffset}/{resource.Size}");
            }
            else
            {
                response.StatusCode = 200;
            }

            response.ContentLength = toByteOffset - fromByteOffset + 1;

            return response.SendFileAsync(
                resource,
                fromByteOffset,
                response.ContentLength);
        }
    }
}
