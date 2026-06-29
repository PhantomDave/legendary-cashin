using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WhereIsMyMoney.Api.Models.EnableBankingModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api.Controllers
{
    [ApiController]
    [Route("import")]
    public sealed class ImportController(
        EnableBankingStore store,
        EnableBankingAuthStateService authStateService,
        EnableBankingImporter importer,
        IOptions<EnableBankingOptions> options) : ApiControllerBase
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
            if (request.Countries.Count == 0)
            {
                return BadRequest(new { error = "At least one country is required." });
            }

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
            if (request.SelectedCountries.Count == 0)
            {
                return BadRequest(new { error = "At least one country is required." });
            }

            if (request.SelectedAspsps.Count == 0)
            {
                return BadRequest(new { error = "At least one ASPSP is required." });
            }

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
            string asps = string.Join(',', request.SelectedAspsps);

            // Store the configuration including countries and selected banks
            bool success = await store.UpdateAsync(id, new EnableBanking
            {
                Id = id,
                AccountId = accountId,
                Asps = asps,
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
            string redirectUrl = BuildRedirectUrl();
            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                return Problem("Enable Banking redirect URL is not configured.", statusCode: 500);
            }

            long accountId = GetAccountId();
            EnableBankingIntegration? enableBanking = await store.GetIntegrationById(accountId, id);
            if (enableBanking is null)
                return NotFound();

            string state = authStateService.CreateState(id, accountId);
            int maxConsentValidity = Math.Max(1, options.Value.MaxConsentValidity);
            string psuType = string.IsNullOrWhiteSpace(options.Value.PsuType) ? "personal" : options.Value.PsuType;
            try
            {
                StartBankAuthApiResponse result = await enableBanking.StartBankAuthAsync(
                    request.AspspName,
                    request.AspspCountry,
                    redirectUrl,
                    state,
                    maxConsentValidity,
                    psuType);
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

        [HttpPost("enablebanking/sessions/{id:long}/start-import")]
        public async Task<IActionResult> StartImport(long id, [FromBody] StartBankSessionImportRequest request)
        {
            long accountId = GetAccountId();
            EnableBankingBankSession? session = await store.GetBankSessionAsync(id);
            if (session is null || session.AccountId != accountId)
                return NotFound();

            Guid jobId = importer.EnqueueImportRequest(
                from: request.From,
                to: request.To,
                sessionId: id,
                accountId: accountId,
                trigger: "manual-session-import");

            return Accepted(new
            {
                message = "Import job queued",
                jobId,
                state = "Queued"
            });
        }

        [HttpGet("jobs/{jobId:guid}")]
        public IActionResult GetImportJobStatus(Guid jobId)
        {
            long accountId = GetAccountId();
            ImportJobStatus? status = importer.GetImportJobStatus(jobId);
            if (status is null || status.AccountId != accountId)
                return NotFound();

            return Ok(status);
        }

        [HttpPost("enablebanking/{id:long}/force-sync")]
        public async Task<IActionResult> PostForceSyncAsync(long id, [FromBody] ForceSyncRequest request)
        {
            string redirectUrl = BuildRedirectUrl();
            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                return Problem("Enable Banking redirect URL is not configured.", statusCode: 500);
            }

            long accountId = GetAccountId();
            EnableBankingIntegration? enableBanking = await store.GetIntegrationById(accountId, id);
            if (enableBanking is null)
                return NotFound();

            string state = authStateService.CreateForceSyncState(id, accountId, request.StartDate, request.EndDate);
            int maxConsentValidity = Math.Max(1, options.Value.MaxConsentValidity);
            string psuType = string.IsNullOrWhiteSpace(options.Value.PsuType) ? "personal" : options.Value.PsuType;

            try
            {
                StartBankAuthApiResponse result = await enableBanking.StartBankAuthAsync(
                    request.AspspName,
                    request.AspspCountry,
                    redirectUrl,
                    state,
                    maxConsentValidity,
                    psuType);
                return Ok(new { url = result.Url, authorizationId = result.AuthorizationId, state });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("enablebanking/complete-force-sync")]
        public async Task<IActionResult> PostCompleteForceSyncAsync([FromBody] CompleteForceSyncRequest request)
        {
            long accountId = GetAccountId();
            (long IntegrationId, long AccountId, DateTime StartDate, DateTime EndDate)? forceSyncData = authStateService.ConsumeForceSyncState(request.State);
            if (forceSyncData is null)
                return BadRequest(new { error = "Invalid or expired state. Please start the Force Sync again." });
            if (forceSyncData.Value.AccountId != accountId)
                return Forbid();

            EnableBankingIntegration? enableBanking = await store.GetIntegrationById(accountId, forceSyncData.Value.IntegrationId);
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
                    IntegrationId = forceSyncData.Value.IntegrationId,
                    AccountId = accountId,
                    SessionId = session.SessionId,
                    AspspName = session.Aspsp.Name,
                    AspspCountry = session.Aspsp.Country,
                    ValidUntil = session.Access.ValidUntil,
                    AccountsJson = System.Text.Json.JsonSerializer.Serialize(accountUids),
                };

                EnableBankingBankSession createdSession = await store.CreateBankSessionAsync(bankSession);
                Guid jobId = importer.EnqueueImportRequest(
                    from: forceSyncData.Value.StartDate,
                    to: forceSyncData.Value.EndDate,
                    sessionId: createdSession.Id,
                    accountId: accountId,
                    trigger: "force-sync");

                return Accepted(new
                {
                    message = "Force Sync authorized successfully. Import queued.",
                    sessionId = createdSession.Id,
                    jobId,
                    state = "Queued"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private string BuildRedirectUrl()
        {
            string redirectUrl = options.Value.RedirectUrl?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(redirectUrl))
                return string.Empty;

            return redirectUrl;
        }
    }
    public record StartConfigurationRequest(IReadOnlyList<string> Countries);
    public record ConfigureAspspsRequest(IReadOnlyList<string> SelectedAspsps, IReadOnlyList<string> SelectedCountries);
    public record StartBankAuthRequest(string AspspName, string AspspCountry);
    public record CompleteBankAuthRequest(string Code, string State);
    public record StartBankSessionImportRequest(DateTime? From, DateTime? To);
    public record ForceSyncRequest(string AspspName, string AspspCountry, DateTime StartDate, DateTime EndDate);
    public record CompleteForceSyncRequest(string Code, string State);
}
