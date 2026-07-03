using Scalar.AspNetCore;
using Serilog;
using Orbit.Application.Constants;
using Orbit.Shared.Constants;
using Orbit.WebApi.Extensions;
using Orbit.WebApi.Hubs;
using Orbit.WebApi.Middlewares;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

var frontendUrl = Environment.GetEnvironmentVariable(EnvironmentConstants.FrontendUrl);
var frontendUrlDev = Environment.GetEnvironmentVariable(EnvironmentConstants.FrontendUrlDev);

var frontendUrls = new List<string>();
if (!string.IsNullOrWhiteSpace(frontendUrl)) frontendUrls.Add(frontendUrl);
if (!string.IsNullOrWhiteSpace(frontendUrlDev) && frontendUrlDev != frontendUrl) frontendUrls.Add(frontendUrlDev);

if (frontendUrls.Count == 0)
    frontendUrls.Add("http://localhost:4200");

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(), "logs", "log.txt"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddInfrastructure();
builder.Services.AddAuth();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins([.. frontendUrls])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseMiddleware<ErrorHandlerMiddleware>();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/hubs"))
    {
        var token = context.Request.Query["access_token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(token))
        {
            context.Request.Headers["Authorization"] = $"Bearer {token}";
        }
    }
    await next();
});

app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Orbit API").WithTheme(ScalarTheme.Purple);
    });
}

app.Run();
