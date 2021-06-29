using Microsoft.AspNetCore.Http;
using Swizzle.Models;

namespace Swizzle.Services
{
    public static class IngestionServiceExtensions
    {
        public static ItemCollection GetCollection(
            this IngestionService ingestionService,
            HttpRequest httpRequest)
            => ingestionService.GetCollectionOrDefault(httpRequest.Host.Host);
    }
}
