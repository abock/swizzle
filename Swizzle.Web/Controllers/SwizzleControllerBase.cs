using Microsoft.AspNetCore.Mvc;

namespace Swizzle.Controllers
{
    public abstract class SwizzleControllerBase : ControllerBase
    {
        protected void ConfigureNoCache()
        {
            Response.Headers.Add("Cache-Control", "no-store,no-cache");
            Response.Headers.Add("Pragma", "no-cache");
        }
    }
}
