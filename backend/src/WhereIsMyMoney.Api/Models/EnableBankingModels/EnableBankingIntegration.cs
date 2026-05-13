using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace WhereIsMyMoney.Api.Models.EnableBankingModels;


public class EnableBankingIntegration(
    string applicationId,
    string certificate) : EnableBanking
{
    public string ApplicationId { get; set; } = applicationId;
    public string Certificate { get; set; } = certificate;
    private readonly HttpClient _http = new() { BaseAddress = new Uri("https://api.enablebanking.com/") };

    private EnableBankingApplicationResponse? enableBankingApplicationResponse;
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            string jwt = CreateJwt();

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            await GetApplicationAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error testing EnableBanking integration: {ex.Message}");
            return false;
        }
    }

    public async Task<EnableBankingApplicationResponse> GetApplicationAsync()
    {
        HttpResponseMessage response = await _http.GetAsync("/application");
        await EnsureSuccessAsync(response);
        string json = await response.Content.ReadAsStringAsync();
        enableBankingApplicationResponse = JsonSerializer.Deserialize<EnableBankingApplicationResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize EnableBanking application response.");
        return enableBankingApplicationResponse;
    }

    private string CreateJwt()
    {
        using RSA rsa = RSA.Create();

        // Normalize certificate: handle escaped newlines, CRLF, and whitespace
        string normalizedCert = Certificate.Replace("\\n", "\n").Replace("\r\n", "\n").Trim();
        rsa.ImportFromPem(normalizedCert);

        RSAParameters keyParams = rsa.ExportParameters(false); // Public parameters only
        SigningCredentials signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };

        DateTime now = DateTime.UtcNow;
        JwtSecurityToken jwt = new JwtSecurityToken(
            audience: "api.enablebanking.com",
            issuer: "enablebanking.com",
            claims: [new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)],
            expires: now.AddMinutes(30),
            signingCredentials: signingCredentials);

        jwt.Header.Add("kid", ApplicationId);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Enable Banking API error {(int)response.StatusCode}: {body}");
        }
    }

    public async Task<IReadOnlyList<AspspData>> StartConfigurationAsync(string[] countries)
    {
        await EnsureAuthenticatedAsync();
        return await GetAspspsAsync(countries);
    }

    public async Task<IReadOnlyList<AspspData>> GetAspspsAsync(string[] countries)
    {
        string countryQuery = string.Join("&country=", countries);
        HttpResponseMessage response = await _http.GetAsync($"/aspsps?country={countryQuery}");
        await EnsureSuccessAsync(response);
        string json = await response.Content.ReadAsStringAsync();
        GetAspspsResponse? response_data = JsonSerializer.Deserialize<GetAspspsResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize EnableBanking ASPSP response.");
        return response_data.Aspsps;
    }

    public void RedactCertificate()
    {
        Certificate = "[REDACTED]";
    }

    public async Task<StartBankAuthApiResponse> StartBankAuthAsync(
        string aspspName,
        string aspspCountry,
        string redirectUrl,
        string state,
        int maxConsentValidityDays,
        string psuType)
    {
        await EnsureAuthenticatedAsync();

        var body = new
        {
            access = new { valid_until = DateTime.UtcNow.AddDays(maxConsentValidityDays).ToString("o") },
            aspsp = new { name = aspspName, country = aspspCountry },
            state,
            redirect_url = redirectUrl,
            psu_type = psuType
        };

        string json = System.Text.Json.JsonSerializer.Serialize(body);
        HttpContent content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _http.PostAsync("/auth", content);
        await EnsureSuccessAsync(response);
        string responseJson = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<StartBankAuthApiResponse>(responseJson)
            ?? throw new InvalidOperationException("Failed to deserialize StartBankAuth response.");
    }

    public async Task<AuthorizeSessionApiResponse> AuthorizeSessionAsync(string code)
    {
        await EnsureAuthenticatedAsync();

        var body = new { code };
        string json = System.Text.Json.JsonSerializer.Serialize(body);
        HttpContent content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _http.PostAsync("/sessions", content);
        await EnsureSuccessAsync(response);
        string responseJson = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<AuthorizeSessionApiResponse>(responseJson)
            ?? throw new InvalidOperationException("Failed to deserialize AuthorizeSession response.");
    }

    public async Task<EnableBankingHalTransactions> GetTransactionsAsync(
        string accountUid,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null)
    {
        await EnsureAuthenticatedAsync();

        System.Text.StringBuilder sb = new($"/accounts/{accountUid}/transactions");
        List<string> query = [];
        if (dateFrom.HasValue) query.Add($"date_from={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue) query.Add($"date_to={dateTo.Value:yyyy-MM-dd}");
        if (query.Count > 0) sb.Append('?').Append(string.Join('&', query));

        HttpResponseMessage response = await _http.GetAsync(sb.ToString());
        await EnsureSuccessAsync(response);
        string responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EnableBankingHalTransactions>(responseJson)
            ?? throw new InvalidOperationException("Failed to deserialize transactions response.");
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (!await AuthenticateAsync())
            throw new InvalidOperationException("Failed to authenticate with Enable Banking API.");
    }
}
