using System.IO;

using Microsoft.AspNetCore.Mvc;

using Swizzle.Services;

namespace Swizzle.Controllers
{
    [Route("~/keybase.txt")]
    [Route("~/.well-known/keybase.txt")]
    [ApiController]
    public sealed class KeybaseController : SwizzleControllerBase
    {
        readonly IngestionService _ingestionService;

        public KeybaseController(IngestionService ingestionService)
            => _ingestionService = ingestionService;

        [HttpGet]
        public ActionResult Get()
        {
            var (items, _) = _ingestionService.GetCollection(Request);
            var keybasePath = Path.Combine(
                _ingestionService.ContentRootPath,
                items.Key + ".keybase.txt");
            return File(System.IO.File.OpenRead(keybasePath), "text/plain");
        }
    }
}
