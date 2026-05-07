using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using WhereIsMyMoney.Import.Models;

#pragma warning disable IDE0008 // Use explicit type instead of 'var'

namespace WhereIsMyMoney.Import.Services;

public sealed class EnableBankingService
{
    private readonly HttpClient _http;
    private readonly string _applicationId;
    private readonly string _privateKeyPath;
    private readonly string _redirectUrl;
    private readonly string _psuType;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public EnableBankingService(
        string apiBaseUrl,
        string applicationId,
        string privateKeyPath,
        string redirectUrl,
        string psuType)
    {
        _applicationId = applicationId;
        _privateKeyPath = privateKeyPath;
        _redirectUrl = redirectUrl;
        _psuType = psuType;

        _http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        RefreshJwt();
    }

    // ── JWT ───────────────────────────────────────────────────────────────────

    public void RefreshJwt()
    {
        string token = CreateJwt();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private string CreateJwt()
    {
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(_privateKeyPath));

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

        jwt.Header.Add("kid", _applicationId);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    // ── Application ───────────────────────────────────────────────────────────

    public async Task<ApplicationResponse> GetApplicationAsync()
    {
        var response = await _http.GetAsync("/application");
        await EnsureSuccessAsync(response);
        return (await DeserializeAsync<ApplicationResponse>(response))!;
    }

    // ── ASPSPs ────────────────────────────────────────────────────────────────

    public async Task<List<AspspData>> GetAspspsAsync(string? country = null)
    {
        string url = "/aspsps";
        if (country is not null)
            url += $"?country={Uri.EscapeDataString(country)}";

        var response = await _http.GetAsync(url);
        await EnsureSuccessAsync(response);
        var result = await DeserializeAsync<GetAspspsResponse>(response);
        return result!.Aspsps;
    }

    // ── Authorization ─────────────────────────────────────────────────────────

    public async Task<StartAuthorizationResponse> StartAuthorizationAsync(Aspsp aspsp, string? redirectUrlOverride = null)
    {
        var body = new StartAuthorizationRequest(
            Access: new Access(
                ValidUntil: DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz"),
                Balances: true,
                Transactions: true),
            Aspsp: aspsp,
            State: Guid.NewGuid().ToString(),
            RedirectUrl: redirectUrlOverride ?? _redirectUrl,
            PsuType: _psuType);

        var response = await PostJsonAsync("/auth", body);
        await EnsureSuccessAsync(response);
        return (await DeserializeAsync<StartAuthorizationResponse>(response))!;
    }

    // ── Session ───────────────────────────────────────────────────────────────

    public async Task<AuthorizeSessionResponse> AuthorizeSessionAsync(string code)
    {
        var response = await PostJsonAsync("/sessions", new AuthorizeSessionRequest(code));
        await EnsureSuccessAsync(response);
        return (await DeserializeAsync<AuthorizeSessionResponse>(response))!;
    }

    // ── Accounts ──────────────────────────────────────────────────────────────

    public async Task<HalBalances> GetBalancesAsync(string accountUid)
    {
        var response = await _http.GetAsync($"/accounts/{accountUid}/balances");
        await EnsureSuccessAsync(response);
        return (await DeserializeAsync<HalBalances>(response))!;
    }

    public async Task<HalTransactions> GetTransactionsAsync(
        string accountUid,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null)
    {
        var sb = new StringBuilder($"/accounts/{accountUid}/transactions");
        var query = new List<string>();
        if (dateFrom.HasValue) query.Add($"date_from={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue) query.Add($"date_to={dateTo.Value:yyyy-MM-dd}");
        if (query.Count > 0) sb.Append('?').Append(string.Join('&', query));

        var response = await _http.GetAsync(sb.ToString());
        await EnsureSuccessAsync(response);
        return (await DeserializeAsync<HalTransactions>(response))!;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> PostJsonAsync<T>(string path, T body)
    {
        string json = JsonSerializer.Serialize(body, JsonOptions);
        return await _http.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));
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

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
