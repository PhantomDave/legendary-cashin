using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api;
using WhereIsMyMoney.Api.Data;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<AccountStore>();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    AccountDbContext db = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    await db.Database.MigrateAsync();
}

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("WhereIsMyMoney.Api API");
});
app.MapControllers();

app.Run();
