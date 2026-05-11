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

    public async Task<bool> TestAsync()
    {
        try
        {
            string jwt = CreateJwt();
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            EnableBankingApplicationResponse app = await GetApplicationAsync();
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
        return JsonSerializer.Deserialize<EnableBankingApplicationResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize EnableBanking application response.");
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
}
