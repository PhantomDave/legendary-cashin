using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using WhereIsMyMoney.Api;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDataProtection();
builder.Services.AddScoped<AccountStore>();
builder.Services.AddScoped<EncryptionService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<BudgetStore>();
builder.Services.AddScoped<TransactionStore>();
builder.Services.AddScoped<CategoryStore>();
builder.Services.AddScoped<EnableBankingStore>();

IConfigurationSection jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped<RecurrenceEngine>();
builder.Services.AddScoped<RecurringTransactionStore>();
builder.Services.AddHostedService<ScheduledTransactionProcessor>();


WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors(policy => policy
    .WithOrigins("http://localhost:4200")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("WhereIsMyMoney.Api API");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

app.Run();
