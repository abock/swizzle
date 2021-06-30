using System.Text.Json;
using System.Text.Json.Serialization;

namespace Swizzle
{
    public static class SwizzleJsonSerializerOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
    }
}
