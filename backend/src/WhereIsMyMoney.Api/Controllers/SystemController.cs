using Microsoft.AspNetCore.Mvc;

namespace WhereIsMyMoney.Api.Controllers;

[ApiController]
[Route("")]
public sealed class SystemController : ControllerBase
{
    [HttpGet]
    public IActionResult GetServiceInfo()
    {
        return Ok(new
        {
            service = "WhereIsMyMoney.Api",
            description = "REST API for receiving and retrieving cashin requests.",
            endpoints = new[]
            {
                "GET /health",
                "GET /cashins",
                "GET /cashins/{id}",
                "POST /cashins"
            },
            openApi = new
            {
                json = "/openapi/v1.json",
                ui = "/scalar/v1"
            }
        });
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "Healthy",
            checkedAtUtc = DateTimeOffset.UtcNow
        });
    }
}

