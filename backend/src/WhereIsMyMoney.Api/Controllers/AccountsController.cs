using Microsoft.AspNetCore.Mvc;

namespace WhereIsMyMoney.Api.Controllers;

[ApiController]
[Route("accounts")]
public sealed class AccountController(AccountStore store) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> GetById(long id)
    {
        AccountResponse? account = await store.GetAsync(id);

        return account is null
            ? NotFound(new { message = $"Account '{id}' was not found." })
            : Ok(account);
    }

    [HttpPost]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountResponse>> Create([FromBody] CreateAccountRequest request)
    {
        AccountResponse createdAccount = await store.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = createdAccount.Id }, createdAccount);
    }
}

