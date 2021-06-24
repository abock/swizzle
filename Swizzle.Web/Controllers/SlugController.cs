using Microsoft.AspNetCore.Mvc;

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
            var items = _ingestionService.GetCollection(Request.Host.Host);

            if (slug == "random")
            {
                ConfigureNoCache();
                if (format is not null)
                    format = "." + format;
                return Redirect($"/{items.Random().Slug}{format}");
            }

            if (items.TryGetItemBySlug(slug, out var item))
                return Ok(item);

            // In legacy shart, as a hint for the rewriter in nginx,
            // each shart vhost's short links all started with a path
            // '/<vhostFirstChar>' such as '/<c>SLUG' for <c>atoverflow.com
            // with the slug SLUG, so support this as well to preserve
            // any old links.
            if (slug.Length > 1 &&
                slug[0] == items.Key[0] &&
                items.TryGetItemBySlug(slug[1..], out item))
                return Ok(item);

            return NotFound();
        }
    }
}
