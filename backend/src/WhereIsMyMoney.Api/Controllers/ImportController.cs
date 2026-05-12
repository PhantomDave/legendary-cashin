using Microsoft.AspNetCore.Mvc;
using WhereIsMyMoney.Api.Models.EnableBankingModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api.Controllers
{
    [ApiController]
    [Route("import")]
    public sealed class ImportController(EnableBankingStore store, EnableBankingAuthStateService authStateService) : ApiControllerBase
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

            if (await integration.AuthenticateAsync())
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

        [HttpGet("enablebanking/integrations")]
        public async Task<IActionResult> GetIntegrations()
        {
            long accountId = GetAccountId();
            IReadOnlyList<EnableBankingIntegration> integrations = await store.GetAllIntegrationsAsync(accountId);
            foreach (EnableBankingIntegration integration in integrations)
            {
                integration.RedactCertificate();
            }
            return Ok(integrations);
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

        [HttpPost("enablebanking/{id:long}/start-configuration")]
        public async Task<IActionResult> StartConfiguration(long id, [FromBody] StartConfigurationRequest request)
        {
            long accountId = GetAccountId();

            // Verify the config belongs to this account
            EnableBankingIntegration? enableBanking = await store.GetIntegrationById(accountId, id);
            if (enableBanking is null || enableBanking.AccountId != accountId)
            {
                return NotFound();
            }

            // Start the configuration process - fetch available ASPSPs for the selected countries
            IReadOnlyList<AspspData>? aspspData = await enableBanking.StartConfigurationAsync([.. request.Countries]);
            return aspspData.Count > 0
                ? Ok(aspspData)
                : BadRequest(new { error = "Failed to start EnableBanking configuration." });
        }

        [HttpPost("enablebanking/{id:long}/configure-aspsps")]
        public async Task<IActionResult> ConfigureAspsps(long id, [FromBody] ConfigureAspspsRequest request)
        {
            long accountId = GetAccountId();

            // Verify the integration belongs to this account
            EnableBankingIntegration? enableBanking = await store.GetIntegrationById(accountId, id);
            if (enableBanking is null || enableBanking.AccountId != accountId)
            {
                return NotFound();
            }

            // Prepare the configuration update with selected ASPSPs and countries
            var configData = new { Countries = request.SelectedCountries };
            string configurationJson = System.Text.Json.JsonSerializer.Serialize(configData);

            // Store the configuration including countries and selected banks
            bool success = await store.UpdateAsync(id, new EnableBanking
            {
                Id = id,
                AccountId = accountId,
                Asps = request.SelectedAspsps,
                CreatedAtUtc = enableBanking.CreatedAtUtc,
                Configuration = configurationJson
            });

            if (!success)
            {
                return BadRequest(new { error = "Failed to configure ASPSPs." });
            }

            return Ok(new { message = "ASPSPs configured successfully", integrationId = id });
        }

        [HttpPost("enablebanking/{id:long}/start-bank-auth")]
        public async Task<IActionResult> StartBankAuth(long id, [FromBody] StartBankAuthRequest request)
        {
            long accountId = GetAccountId();
            EnableBankingIntegration? enableBanking = await store.GetIntegrationById(accountId, id);
            if (enableBanking is null)
                return NotFound();

            string state = authStateService.CreateState(id, accountId);
            try
            {
                StartBankAuthApiResponse result = await enableBanking.StartBankAuthAsync(
                    request.AspspName, request.AspspCountry, request.RedirectUrl, state);
                return Ok(new { url = result.Url, authorizationId = result.AuthorizationId, state });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("enablebanking/complete-bank-auth")]
        public async Task<IActionResult> CompleteBankAuth([FromBody] CompleteBankAuthRequest request)
        {
            long accountId = GetAccountId();
            (long IntegrationId, long AccountId)? stateData = authStateService.ConsumeState(request.State);
            if (stateData is null)
                return BadRequest(new { error = "Invalid or expired state. Please start the bank connection again." });
            if (stateData.Value.AccountId != accountId)
                return Forbid();

            EnableBankingIntegration? enableBanking = await store.GetIntegrationById(accountId, stateData.Value.IntegrationId);
            if (enableBanking is null)
                return NotFound();

            try
            {
                AuthorizeSessionApiResponse session = await enableBanking.AuthorizeSessionAsync(request.Code);

                IEnumerable<string> accountUids = session.Accounts
                    .Where(a => a.Uid is not null)
                    .Select(a => a.Uid!);

                EnableBankingBankSession bankSession = new()
                {
                    IntegrationId = stateData.Value.IntegrationId,
                    AccountId = accountId,
                    SessionId = session.SessionId,
                    AspspName = session.Aspsp.Name,
                    AspspCountry = session.Aspsp.Country,
                    ValidUntil = session.Access.ValidUntil,
                    AccountsJson = System.Text.Json.JsonSerializer.Serialize(accountUids),
                };

                await store.CreateBankSessionAsync(bankSession);
                return Ok(new { message = "Bank connected successfully", sessionId = session.SessionId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("enablebanking/sessions")]
        public async Task<IActionResult> GetBankSessions()
        {
            long accountId = GetAccountId();
            IReadOnlyList<EnableBankingBankSession> sessions = await store.GetBankSessionsByAccountIdAsync(accountId);
            return Ok(sessions);
        }

        [HttpDelete("enablebanking/sessions/{id:long}")]
        public async Task<IActionResult> DeleteBankSession(long id)
        {
            long accountId = GetAccountId();
            EnableBankingBankSession? session = await store.GetBankSessionAsync(id);
            if (session is null || session.AccountId != accountId)
                return NotFound();
            await store.DeleteBankSessionAsync(id);
            return NoContent();
        }
    }

    public record StartConfigurationRequest(IReadOnlyList<string> Countries);
    public record ConfigureAspspsRequest(string SelectedAspsps, string SelectedCountries);
    public record StartBankAuthRequest(string AspspName, string AspspCountry, string RedirectUrl);
    public record CompleteBankAuthRequest(string Code, string State);
}
