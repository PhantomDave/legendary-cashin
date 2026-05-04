using WhereIsMyMoney.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<CashinStore>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("WhereIsMyMoney.Api API");
});
app.MapControllers();

app.Run();


public partial class Program;

