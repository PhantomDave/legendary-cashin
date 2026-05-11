using Microsoft.AspNetCore.Mvc;
using WhereIsMyMoney.Api.Models.EnableBankingModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api.Controllers
{
    [ApiController]
    [Route("import")]
    public sealed class ImportController(EnableBankingStore store) : ApiControllerBase
    {
        [HttpPost("enablebanking")]
        public async Task<IActionResult> Import([FromBody] CreateEnableBankingRequest request)
        {
            long accountId = GetAccountId();

            // Create and test the integration
            EnableBankingIntegration integration = new(request.ApplicationId, request.Certificate)
            {
                AccountId = accountId,
                Asps = request.Asps
            };

            if (await integration.TestAsync())
            {
                // Store the configuration
                await store.CreateIntegrationAsync(integration);
                return Ok(new { message = "EnableBanking integration configured successfully" });
            }
            else
            {
                return BadRequest(new { error = "Failed to connect to EnableBanking API. Please check your credentials." });
            }
        }

        [HttpGet("enablebanking")]
        public async Task<IActionResult> GetConfigurations()
        {
            long accountId = GetAccountId();
            IReadOnlyList<EnableBanking> configs = await store.GetAllByAccountId(accountId);
            return Ok(configs);
        }

        [HttpDelete("enablebanking/{id:long}")]
        public async Task<IActionResult> DeleteConfiguration(long id)
        {
            long accountId = GetAccountId();

            // Verify the config belongs to this account
            EnableBanking? config = await store.GetAsync(id);
            if (config is null || config.AccountId != accountId)
            {
                return NotFound();
            }

            await store.DeleteAsync(id);
            return NoContent();
        }
    }
}
