using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace WhereIsMyMoney.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected long GetAccountId() =>
        long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!);
}
