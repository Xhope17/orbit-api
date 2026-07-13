using Scalar.AspNetCore;
using Serilog;
using Orbit.Application.Constants;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.DataBase;
using Orbit.Domain.Interfaces.Repositories;
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
var frontendUrlAlt = Environment.GetEnvironmentVariable(EnvironmentConstants.FrontendUrlAlt);

var frontendUrls = new List<string>();
if (!string.IsNullOrWhiteSpace(frontendUrl)) frontendUrls.Add(frontendUrl);
if (!string.IsNullOrWhiteSpace(frontendUrlDev) && frontendUrlDev != frontendUrl) frontendUrls.Add(frontendUrlDev);
if (!string.IsNullOrWhiteSpace(frontendUrlAlt) && frontendUrlAlt != frontendUrl && frontendUrlAlt != frontendUrlDev) frontendUrls.Add(frontendUrlAlt);

if (frontendUrls.Count == 0)
    frontendUrls.Add("http://localhost:4200");

Log.Logger.Information("CORS origins: {Origins}", frontendUrls);

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(), "logs", "log.txt"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddInfrastructure();
builder.Services.AddAuth(builder.Configuration);

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
    Console.WriteLine("Scalar: http://localhost:5230/scalar");
}

using (var scope = app.Services.CreateScope())
{
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    var anyUser = await uow.authUserRepository.Get(u => u.Id != Guid.Empty);
    if (anyUser is null)
    {
        var result = await authService.RegisterAsync(
            "firstuser@gmail.com", "admin", "Admin",
            "Adminadmin1", null, null, null
        );

        if (result.IsSuccess)
        {
            var profile = await uow.profileRepository.Get(p => p.Username == "admin");
            var adminRole = await uow.roleRepository.Get(r => r.Name == "admin");

            if (profile is not null && adminRole is not null)
            {
                var existingUserRole = await uow.userRoleRepository.Get(ur =>
                    ur.ProfileId == profile.Id && ur.RoleId == adminRole.Id);
                if (existingUserRole is null)
                {
                    var adminAssignment = new Orbit.Domain.Entities.UserRole
                    {
                        Id = Guid.NewGuid(),
                        ProfileId = profile.Id,
                        RoleId = adminRole.Id,
                        AssignedAt = DateTime.UtcNow,
                    };
                    await uow.userRoleRepository.Create(adminAssignment);
                    await uow.SaveChangesAsync();
                }
            }
        }
    }
}

app.Run();
