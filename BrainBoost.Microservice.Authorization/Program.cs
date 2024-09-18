using Keycloak.Auth.Api.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information($"Starting application: {typeof(Program).Assembly.GetName().Name}");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGenWithAuth(builder.Configuration);
    
    builder.Services.AddAuthorization();
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.RequireHttpsMetadata = false;
            o.Audience = builder.Configuration["Authentication:Audience"];
            o.MetadataAddress = builder.Configuration["Authentication:MetadataAddress"]!;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = builder.Configuration["Authentication:ValidIssuer"]
            };  
        });

    builder.Services.AddJaeger();
    builder.Services.AddPrometheus();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseOpenTelemetryPrometheusScrapingEndpoint();

    app.MapPrometheusScrapingEndpoint();

    app.UseHttpsRedirection();

    app.MapGet("users/me", (ClaimsPrincipal claimsPrincipal) =>
    {
        return claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
    }).RequireAuthorization();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}