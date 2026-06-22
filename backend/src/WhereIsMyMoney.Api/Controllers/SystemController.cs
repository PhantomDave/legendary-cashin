using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WhereIsMyMoney.Api.Controllers
{
    [ApiController]
    [Route("")]
    public sealed class SystemController : ControllerBase
    {
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                status = "Healthy",
                checkedAtUtc = DateTimeOffset.UtcNow
            });
        }
    }

}
