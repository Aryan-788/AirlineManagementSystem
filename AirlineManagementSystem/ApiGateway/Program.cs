using Serilog;
using Serilog.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MMLib.SwaggerForOcelot;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Http;
using System;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Shared.Security;
using Shared.Middleware;
using Shared.Extensions;
using Shared.Handlers;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [UserId:{UserId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [UserId:{UserId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("ocelot.SwaggerEndPoints.json", optional: false, reloadOnChange: true);

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["Key"] ?? "your-super-secret-key-that-is-at-least-32-characters-long";
var jwtIssuer = jwtSettings["Issuer"] ?? "AirlineIdentityService";
var jwtAudiences = jwtSettings.GetSection("Audiences").Get<List<string>>() ?? new List<string> { jwtSettings["Audience"] ?? "AirlineManagementSystem" };

builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddSwaggerForOcelot(builder.Configuration);

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider provider)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("Polly.Retry");
                logger?.LogWarning("Polly Retry {RetryAttempt} after {TimeSpan}s. Exception: {ExceptionMessage}. StatusCode: {StatusCode}", 
                    retryAttempt, timespan.Seconds, outcome.Exception?.Message, outcome.Result?.StatusCode);
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IServiceProvider provider)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TimeoutException>()
        .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30),
            onBreak: (outcome, timespan) =>
            {
                var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("Polly.CircuitBreaker");
                logger?.LogError("Polly Circuit broken for {TimeSpan}s. Exception: {ExceptionMessage}. StatusCode: {StatusCode}", 
                    timespan.TotalSeconds, outcome.Exception?.Message, outcome.Result?.StatusCode);
            },
            onReset: () =>
            {
                var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("Polly.CircuitBreaker");
                logger?.LogInformation("Polly Circuit reset.");
            });
}

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(10);
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationHttpHandler>();

builder.Services.ConfigureAll<HttpClientFactoryOptions>(options =>
{
    options.HttpMessageHandlerBuilderActions.Add(b =>
    {
        b.AdditionalHandlers.Add(b.Services.GetRequiredService<CorrelationHttpHandler>());
        b.AdditionalHandlers.Add(new PolicyHttpMessageHandler(GetRetryPolicy(b.Services)));
        b.AdditionalHandlers.Add(new PolicyHttpMessageHandler(GetCircuitBreakerPolicy(b.Services)));
        b.AdditionalHandlers.Add(new PolicyHttpMessageHandler(GetTimeoutPolicy()));
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudiences = jwtAudiences,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationHttpHandler>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


var app = builder.Build();

app.UseCorrelationId();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.Use(async (context, next) =>
{
    var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                 ?? context.User?.FindFirst("sub")?.Value
                 ?? context.Request.Headers["X-User-Id"].FirstOrDefault()
                 ?? "Anonymous";

    using (LogContext.PushProperty("UserId", userId))
    {
        await next(context);
    }
});

app.UseSerilogRequestLogging();

app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerForOcelotUI(options =>
{
    options.PathToSwaggerGenerator = "/swagger/docs";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.UseOcelot();

app.Run();


