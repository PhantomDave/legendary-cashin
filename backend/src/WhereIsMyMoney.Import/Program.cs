using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WhereIsMyMoney.Import.Models;
using WhereIsMyMoney.Import.Services;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables("EB_")
    .Build();

string applicationId = config["EnableBanking:ApplicationId"]
    ?? throw new InvalidOperationException("EnableBanking:ApplicationId is required.");
string privateKeyPath = config["EnableBanking:PrivateKeyPath"]
    ?? throw new InvalidOperationException("EnableBanking:PrivateKeyPath is required.");
string apiBaseUrl = config["EnableBanking:ApiBaseUrl"] ?? "https://api.enablebanking.com";
string redirectUrl = config["EnableBanking:RedirectUrl"] ?? "https://localhost:5080/enablebanking";
string psuType = config["EnableBanking:PsuType"] ?? "personal";

if (!File.Exists(privateKeyPath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"Private key not found at: {Path.GetFullPath(privateKeyPath)}");
    Console.Error.WriteLine("Place your Enable Banking private key PEM file at that path and retry.");
    Console.ResetColor();
    return 1;
}

PrintBanner();

var svc = new EnableBankingService(apiBaseUrl, applicationId, privateKeyPath, redirectUrl, psuType);

// ── 1. Verify application ──────────────────────────────
Console.Write("Connecting to Enable Banking... ");
ApplicationResponse app;
try
{
    app = await svc.GetApplicationAsync();
}
catch (Exception ex)
{
    PrintError(ex.Message);
    return 1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("OK");
Console.ResetColor();
Console.WriteLine($"  Application : {app.Name}");
if (app.Description is not null) Console.WriteLine($"  Description : {app.Description}");
Console.WriteLine();

// ── 2. ASPSPs List ────────────────
string? country = Prompt("Filter by country code (e.g. GB, FI, SE) — leave blank for all: ").Trim();
if (country.Length == 0) country = null;

Console.Write("Fetching available banks... ");
List<AspspData> aspsps;
try
{
    aspsps = await svc.GetAspspsAsync(country);
}
catch (Exception ex)
{
    PrintError(ex.Message);
    return 1;
}

if (aspsps.Count == 0)
{
    Console.WriteLine("No banks found for the given filter.");
    return 0;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"{aspsps.Count} banks found");
Console.ResetColor();
Console.WriteLine();

for (int i = 0; i < aspsps.Count; i++)
{
    var a = aspsps[i];
    Console.WriteLine($"  [{i + 1,3}] {a.Country}  {a.Name}{(a.Bic is not null ? $" ({a.Bic})" : "")}");
}
Console.WriteLine();

int selectedIndex = PromptInt("Select bank number: ", 1, aspsps.Count) - 1;
var selectedAspsp = aspsps[selectedIndex];
Console.WriteLine($"Selected: {selectedAspsp.Name} ({selectedAspsp.Country})");
Console.WriteLine();

// authStart is populated after ngrok URL is known
StartAuthorizationResponse authStart;

// ── 3. Start local HTTP callback server + ngrok tunnel ────────────────────────
const int callbackPort = 5080;
const string callbackPath = "/enablebanking";

var tcs = new TaskCompletionSource<(string? Code, string? Error, string? ErrorDescription)>();

Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
Environment.SetEnvironmentVariable("ASPNETCORE_HTTP_PORTS", null);
Environment.SetEnvironmentVariable("ASPNETCORE_HTTPS_PORTS", null);

var webBuilder = WebApplication.CreateSlimBuilder();
webBuilder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(callbackPort));
webBuilder.Logging.SetMinimumLevel(LogLevel.None);

var callbackApp = webBuilder.Build();

callbackApp.MapGet(callbackPath, async (HttpContext ctx) =>
{
    string? code = ctx.Request.Query["code"];
    string? error = ctx.Request.Query["error"];
    string? errorDesc = ctx.Request.Query["error_description"];

    // Write the response before signalling completion so the browser receives it
    ctx.Response.ContentType = "text/html";
    string html = error is not null
        ? "<html><head><style>body{font-family:sans-serif;text-align:center;padding:60px}</style></head>" +
          $"<body><h2>Authorization failed</h2><p>{error}: {errorDesc}</p>" +
          "<p>You can close this tab and return to the terminal.</p></body></html>"
        : "<html><head><style>body{font-family:sans-serif;text-align:center;padding:60px}</style></head>" +
          "<body><h2 style='color:green'>Authorization successful!</h2>" +
          "<p>You can close this tab. Return to the terminal to view your accounts.</p></body></html>";

    await ctx.Response.WriteAsync(html);
    await ctx.Response.CompleteAsync();

    tcs.TrySetResult((code, error, errorDesc));
});

await callbackApp.StartAsync();

// Start ngrok tunnel
Console.Write("Starting ngrok tunnel... ");
Process? ngrokProcess = null;
string publicRedirectUrl;

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("OK");
Console.ResetColor();
Console.WriteLine();

// Check if the ngrok URL is registered — if not, prompt user to add it
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("IMPORTANT: The redirect URL must be registered in your Enable Banking app.");
Console.WriteLine($"  Add this URL at https://enablebanking.com/cp/applications :");
Console.ForegroundColor = ConsoleColor.Cyan;
Console.ResetColor();
Console.WriteLine();
Prompt("Press ENTER once you have saved the redirect URL in Enable Banking... ");
Console.WriteLine();

// Re-start authorization using the ngrok redirect URL
Console.Write("Re-starting authorization with ngrok URL... ");
svc.RefreshJwt();
try
{
    authStart = await svc.StartAuthorizationAsync(new Aspsp(selectedAspsp.Name, selectedAspsp.Country), "https://shortcake-hypocrite-spool.ngrok-free.dev/enablebanking");
}
catch (Exception ex)
{
    PrintError(ex.Message);
    await callbackApp.StopAsync();
    return 1;
}
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("OK");
Console.ResetColor();
Console.WriteLine();

// ── 5. Open browser ───────────────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("Opening browser — complete the bank login, then return here.");
Console.ResetColor();
Console.WriteLine($"  {authStart.Url}");
Console.WriteLine();
Console.WriteLine("Waiting for authorization callback...");

OpenBrowser(authStart.Url);

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
cts.Token.Register(() => tcs.TrySetResult((null, "timeout", "Authorization timed out after 5 minutes.")));

var (authCode, authError, authErrorDesc) = await tcs.Task;
// Give the browser 2 seconds to receive the response before tearing down
await Task.Delay(2000);
ngrokProcess?.Kill(true);
await callbackApp.StopAsync();

if (authError is not null)
{
    PrintError($"Authorization failed: {authError}{(authErrorDesc is not null ? $" — {authErrorDesc}" : "")}");
    return 1;
}

if (string.IsNullOrWhiteSpace(authCode))
{
    PrintError("No authorization code received.");
    return 1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Authorization code received.");
Console.ResetColor();
Console.WriteLine();

// ── 6. Create session ──────────────────────────────────────────────────────────
Console.Write("Creating session... ");
svc.RefreshJwt();
AuthorizeSessionResponse session;
try
{
    session = await svc.AuthorizeSessionAsync(authCode);
}
catch (Exception ex)
{
    PrintError(ex.Message);
    return 1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("OK");
Console.ResetColor();
Console.WriteLine($"  Session ID : {session.SessionId}");
Console.WriteLine($"  Accounts   : {session.Accounts.Count}");
Console.WriteLine();

// ── 7. Show accounts + transactions balances + ────────────────────
foreach (AccountResource account in session.Accounts)
{
    string displayName = account.Name ?? account.AccountId?.Iban ?? account.AccountId?.Other?.Identification ?? "Unknown";
    string iban = account.AccountId?.Iban ?? "-";

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"══ {displayName} ══════════════════════════════════════");
    Console.ResetColor();
    Console.WriteLine($"  IBAN     : {iban}");
    Console.WriteLine($"  Currency : {account.Currency}");
    Console.WriteLine($"  Type     : {account.CashAccountType}");

    if (account.Uid is null)
    {
        Console.WriteLine("  (No UID — balances/transactions not available for this account)");
        Console.WriteLine();
        continue;
    }

    try
    {
        HalBalances balances = await svc.GetBalancesAsync(account.Uid);
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  Balances:");
        Console.ResetColor();
        foreach (BalanceResource b in balances.Balances)
            Console.WriteLine($"    {b.BalanceType,-8} {b.BalanceAmount.Amount,14} {b.BalanceAmount.Currency}  ({b.Name})");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  Balances unavailable: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine();
    string fetchTx = Prompt("  Fetch transactions? (y/N): ").Trim().ToLowerInvariant();
    if (fetchTx is "y" or "yes")
    {
        string fromStr = Prompt("  Date from (yyyy-MM-dd, blank = 30 days ago): ").Trim();
        string toStr = Prompt("  Date to   (yyyy-MM-dd, blank = today):        ").Trim();

        DateOnly? dateFrom = fromStr.Length > 0 ? DateOnly.Parse(fromStr) : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        DateOnly? dateTo = toStr.Length > 0 ? DateOnly.Parse(toStr) : DateOnly.FromDateTime(DateTime.UtcNow);

        try
        {
            HalTransactions tx = await svc.GetTransactionsAsync(account.Uid, dateFrom, dateTo);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  Transactions ({tx.Transactions.Count}):");
            Console.ResetColor();

            if (tx.Transactions.Count == 0)
            {
                Console.WriteLine("    No transactions in range.");
            }
            else
            {
                Console.WriteLine($"    {"Date",-12} {"D/C",-5} {"Amount",12} {"Ccy",-4} Description");
                Console.WriteLine($"    {new string('─', 70)}");
                foreach (Transaction t in tx.Transactions)
                {
                    string date = t.BookingDate ?? t.ValueDate ?? "-";
                    string dir = t.CreditDebitIndicator == "CRDT" ? "IN " : "OUT";
                    string desc = t.RemittanceInformation?.FirstOrDefault()
                        ?? t.CreditorName ?? t.DebtorName ?? "-";

                    Console.ForegroundColor = t.CreditDebitIndicator == "CRDT"
                        ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"    {date,-12} {dir,-5} {t.TransactionAmount.Amount,12} {t.TransactionAmount.Currency,-4} {Truncate(desc, 40)}");
                    Console.ResetColor();
                }

                if (tx.ContinuationKey is not null)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("    (more transactions available — pagination not shown)");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"  Transactions unavailable: {ex.Message}");
            Console.ResetColor();
        }
    }

    Console.WriteLine();
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Done. Session ID for future reference: " + session.SessionId);
Console.ResetColor();
return 0;

// ── Helpers ────────────────────────────────────────────────────────────────────

static void PrintBanner()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("=== Where Is My Money - Bank Import ===");
    Console.WriteLine("    Powered by Enable Banking          ");
    Console.WriteLine("========================================");
    Console.ResetColor();
    Console.WriteLine();
}

static void PrintError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("ERROR: " + message);
    Console.ResetColor();
}

static string Prompt(string message)
{
    Console.Write(message);
    return Console.ReadLine() ?? string.Empty;
}

static int PromptInt(string message, int min, int max)
{
    while (true)
    {
        string input = Prompt(message);
        if (int.TryParse(input, out int value) && value >= min && value <= max)
            return value;
        Console.WriteLine($"Please enter a number between {min} and {max}.");
    }
}

static string Truncate(string s, int max) =>
    s.Length <= max ? s : s[..(max - 1)] + "…";

static async Task<(Process NgrokProcess, string PublicUrl)> StartNgrokAsync(int port, string path)
{
    var psi = new ProcessStartInfo("ngrok", $"http {port} --log=stdout --log-format=json --request-header-add=ngrok-skip-browser-warning:true")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start ngrok process.");

    // Stream ngrok's JSON log lines to the console in the background
    _ = Task.Run(async () =>
    {
        string? line;
        while ((line = await proc.StandardOutput.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                string lvl = root.TryGetProperty("lvl", out var l) ? l.GetString() ?? "info" : "info";
                string msg = root.TryGetProperty("msg", out var m) ? m.GetString() ?? line : line;
                string err = root.TryGetProperty("err", out var e) ? e.GetString() ?? "" : "";

                Console.ForegroundColor = lvl switch
                {
                    "eror" or "crit" => ConsoleColor.Red,
                    "warn" => ConsoleColor.Yellow,
                    "dbug" => ConsoleColor.DarkGray,
                    _ => ConsoleColor.DarkCyan
                };
                Console.Write($"  [ngrok] {msg}");
                if (!string.IsNullOrEmpty(err)) Console.Write($" — {err}");
                Console.WriteLine();
                Console.ResetColor();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"  [ngrok] {line}");
                Console.ResetColor();
            }
        }
    });

    _ = Task.Run(async () =>
    {
        string? line;
        while ((line = await proc.StandardError.ReadLineAsync()) is not null)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [ngrok stderr] {line}");
                Console.ResetColor();
            }
        }
    });

    // Poll ngrok's local API until tunnel is ready (up to 10 seconds)
    using var http = new System.Net.Http.HttpClient();
    string publicUrl = string.Empty;
    for (int i = 0; i < 20; i++)
    {
        await Task.Delay(500);
        try
        {
            string json = await http.GetStringAsync("http://localhost:4040/api/tunnels");
            using var doc = JsonDocument.Parse(json);
            foreach (var tunnel in doc.RootElement.GetProperty("tunnels").EnumerateArray())
            {
                string url = tunnel.GetProperty("public_url").GetString() ?? string.Empty;
                if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    publicUrl = url.TrimEnd('/') + path;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(publicUrl)) break;
        }
        catch { /* not ready yet */ }
    }

    if (string.IsNullOrEmpty(publicUrl))
        throw new InvalidOperationException("ngrok tunnel did not become ready in time. Is ngrok authenticated? Run: ngrok config add-authtoken <your-token>");

    return (proc, publicUrl);
}

static void OpenBrowser(string url)
{
    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
        else
            Process.Start("xdg-open", url);
    }
    catch
    {
        // Best-effort; user can copy the URL manually.
    }
}
