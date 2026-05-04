using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models.AccountModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api.Controllers
{
    [ApiController]
    [Route("accounts")]
    public sealed class AccountController(AccountStore store, TokenService tokenService) : ControllerBase
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

        [HttpGet("{email}")]
        [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AccountResponse>> GetByEmail(string email)
        {
            Account? account = await store.GetByEmailAsync(email);

            return account is null
                ? NotFound(new { message = $"Account with email '{email}' was not found." })
                : Ok(AccountStore.ToResponse(account));
        }

        [HttpPost("authenticate")]
        [AllowAnonymous]
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Authenticate([FromBody] AuthenticateRequest request)
        {
            Account? account = await store.GetByEmailAsync(request.Email);
            if (account is null || !AccountStore.VerifyPassword(request.Password, account.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            string token = tokenService.GenerateToken(account);
            return Ok(new AuthResponse(account.Id, account.Name, account.Email, token));
        }

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AccountResponse>> Create([FromBody] CreateAccountRequest request)
        {
            AccountResponse createdAccount = await store.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = createdAccount.Id }, createdAccount);
        }
    }

}
