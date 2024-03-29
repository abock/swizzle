using System.Linq;
using Microsoft.AspNetCore.Mvc;

using Swizzle.Dto;
using Swizzle.Models;
using Swizzle.Services;

namespace Swizzle.Controllers
{
    [Route("")]
    [ApiController]
    public sealed class SlugController : SwizzleControllerBase
    {
        readonly IngestionService _ingestionService;

        public SlugController(IngestionService ingestionService)
            => _ingestionService = ingestionService;

        [HttpGet("{slug}.{format?}")]
        [FormatFilter]
        public ActionResult GetItemBySlug(
            string slug,
            string? format = null)
        {
            var collection = _ingestionService.GetCollection(
                Request,
                out var baseUri);

            if (slug == "random")
            {
                ConfigureNoCache();
                if (format is not null)
                    format = "." + format;
                return Redirect($"/{collection.Random().Slug}{format}");
            }

            Item? GetItem(string slug)
            {
                if (collection.TryGetItemBySlug(slug, out var item))
                    return item;

                // In legacy shart, as a hint for the rewriter in nginx,
                // each shart vhost's short links all started with a path
                // '/<vhostFirstChar>' such as '/<c>SLUG' for <c>atoverflow.com
                // with the slug SLUG, so support this as well to preserve
                // any old links.
                if (slug.Length > 1 &&
                    slug[0] == collection.Key[0] &&
                    collection.TryGetItemBySlug(slug[1..], out item))
                    return item;

                return null;
            }

            var item = GetItem(slug);
            if (item is null)
                return NotFound();

            if (format is null)
            {
                var uriRedirectResource = item.FindResource(
                    ItemResourceKind.Uri);
                if (uriRedirectResource is not null)
                {
                    var redirectUri = UriList
                        .FromFile(uriRedirectResource.PhysicalPath)
                        .Uris
                        .FirstOrDefault();

                    if (redirectUri is null)
                        return Problem("no redirect URIs in resource");

                    ConfigureNoCache();

                    return Redirect(redirectUri);
                }
            }

            if (format is null ||
                !ItemResourceKind.TryGetFromExtension(format, out _))
                return Ok(item.ToDto(baseUri));

            return Ok(item);
        }
    }
}
