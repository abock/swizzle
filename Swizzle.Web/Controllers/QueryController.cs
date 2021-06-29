using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;

using Swizzle.Dto;
using Swizzle.Services;

namespace Swizzle.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class QueryController : SwizzleControllerBase
    {
        #pragma warning disable CA1069
        public enum QueryOrder
        {
            Descending = 1,
            Desc = 1,
            D = 1,
            Ascending = 2,
            Asc = 2,
            A = 2,
            Random = 3,
            Rand = 3,
            R = 3
        }
        #pragma warning restore CA1069

        readonly IngestionService _ingestionService;

        public QueryController(IngestionService ingestionService)
            => _ingestionService = ingestionService;

        [HttpGet]
        [Produces("text/plain", "application/json")]
        public ActionResult<IEnumerable<ItemDto>> Get(
            QueryOrder order = default,
            int? offset = null,
            int? limit = null,
            bool? render = null)
        {
            ConfigureNoCache();

            var (items, baseUri) = _ingestionService.GetCollection(Request);
            
            var query = items.Where(item => item.Exists);

            query = order switch
            {
                QueryOrder.Ascending => query.OrderBy(item => item.CreationTime),
                QueryOrder.Random => query.Shuffle(),
                _ => query.OrderByDescending(item => item.CreationTime)
            };

            if (offset.HasValue && offset.Value > 0)
                query = query.Skip(offset.Value);

            if (limit.HasValue && limit.Value > 0)
                query = query.Take(limit.Value);

            var itemDtos = query.Select(item => item.ToDto(baseUri));

            if ((render.HasValue && render.Value) ||
                Request.Query.ContainsKey("render"))
            {
                var item = itemDtos.First();
                return Redirect(item.Resources[0].Uri.OriginalString);
            }

            return Ok(itemDtos);
        }
    }
}
