using System;

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
            var items = _ingestionService.GetCollection(Request);

            if (slug == "random")
            {
                ConfigureNoCache();
                if (format is not null)
                    format = "." + format;
                return Redirect($"/{items.Random().Slug}{format}");
            }

            Item? GetItem(string slug)
            {
                if (items.TryGetItemBySlug(slug, out var item))
                    return item;

                // In legacy shart, as a hint for the rewriter in nginx,
                // each shart vhost's short links all started with a path
                // '/<vhostFirstChar>' such as '/<c>SLUG' for <c>atoverflow.com
                // with the slug SLUG, so support this as well to preserve
                // any old links.
                if (slug.Length > 1 &&
                    slug[0] == items.Key[0] &&
                    items.TryGetItemBySlug(slug[1..], out item))
                    return item;

                return null;
            }

            var item = GetItem(slug);
            if (item is null)
                return NotFound();

            if (format is null ||
                !ItemResourceKind.TryGetFromExtension(format, out _))
                return Ok(item.ToDto(new Uri($"https://{Request.Host}/")));

            return Ok(item);
        }
    }
}
