using System.Collections.Generic;
using System.Text;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Swizzle.Dto;
using Swizzle.Models;
using Swizzle.Services;

namespace Swizzle.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    [Authorize]
    public sealed class ItemsController : SwizzleControllerBase
    {
        static readonly UTF8Encoding s_utf8EncodingNoBom = new(false, false);

        readonly IngestionService _ingestionService;

        public ItemsController(IngestionService ingestionService)
        {
            _ingestionService = ingestionService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IEnumerable<ItemDto> Get()
        {
            var collection = _ingestionService.GetCollection(
                Request,
                out var baseUri);

            foreach (var item in collection)
                yield return item.ToDto(baseUri);
        }

        [HttpPost]
        [Route("redirect")]
        public ActionResult<ItemDto> Post(CreateRedirectionItemDto createRequest)
            => CreateItem(createRequest, replaceResource: false);

        [HttpPut]
        [Route("redirect")]
        public ActionResult<ItemDto> Put(CreateRedirectionItemDto createRequest)
            => CreateItem(createRequest, replaceResource: true);

        ActionResult<ItemDto> CreateItem(
            CreateRedirectionItemDto createRequest,
            bool replaceResource)
        {
            try
            {
                var item = _ingestionService
                    .CreateAndIngestFile(
                        Request,
                        ItemResourceKind.Uri,
                        s_utf8EncodingNoBom.GetBytes(
                            createRequest.Target.OriginalString),
                        slug: createRequest.Slug,
                        replaceResource: replaceResource)
                    .ToDto(Request);
                return Created(item.Uri, item);
            }
            catch (ItemAlreadyExistsException)
            {
                return Problem($"Item already exists", statusCode: 409);
            }
        }
    }
}
