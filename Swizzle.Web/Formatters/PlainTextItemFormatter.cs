using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Net.Http.Headers;

using Swizzle.Dto;

namespace Swizzle.Formatters
{
    public sealed class PlainTextItemOutputFormatter : TextOutputFormatter
    {
        public PlainTextItemOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanWriteType(Type type)
            => typeof(ItemDto).IsAssignableFrom(type) ||
                typeof(IEnumerable<ItemDto>).IsAssignableFrom(type);

        public override Task WriteResponseBodyAsync(
            OutputFormatterWriteContext context,
            Encoding selectedEncoding)
        {
            var bufferPool = context.HttpContext
                .RequestServices
                .GetRequiredService<ObjectPool<StringBuilder>>();

            var buffer = bufferPool.Get();

            try
            {
                switch (context.Object)
                {
                    case IEnumerable<ItemDto> items:
                        foreach (var item in items)
                            WriteItem(buffer, item);
                        break;
                    case ItemDto item:
                        WriteItem(buffer, item);
                        break;
                    default:
                        throw new NotSupportedException(
                            $"unsupported type '{context.ObjectType}' " +
                            $"for object: {context.Object}");
                }

                return context.HttpContext.Response.WriteAsync(
                    buffer.ToString(),
                    selectedEncoding);
            }
            finally
            {
                bufferPool.Return(buffer);
            }
        }

        static void WriteItem(StringBuilder buffer, ItemDto item)
            => buffer.Append(item.Uri).Append('\n');
    }
}
