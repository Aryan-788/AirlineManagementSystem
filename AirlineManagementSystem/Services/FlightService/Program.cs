using Shared.Middleware;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;
using Serilog;
using Serilog.Context;
using FlightService.Caching;
using FlightService.Data;
using FlightService.Repositories;
using FlightService.Services;
using FlightService.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using Shared.Configuration;
using Shared.RabbitMQ;
using Shared.Security;
using Shared.Middleware;
using Shared.Extensions;
using Shared.Handlers;
using StackExchange.Redis;
using Shared.Events;

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

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
var rabbitMqSettings = builder.Configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>() ?? new RabbitMqSettings();

builder.Services.AddDbContext<FlightDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(redisConnectionString);
});

// Register RedisCacheService
builder.Services.AddScoped<ICacheService, RedisCacheService>();

builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IFlightService, FlightService.Services.FlightService>();
builder.Services.AddScoped<IFlightScheduleService, FlightScheduleService>();
builder.Services.AddHostedService<ScheduleCompletionWorker>();

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

builder.Services.AddHttpClient("Default")
    .AddHttpMessageHandler<CorrelationHttpHandler>()
    .AddPolicyHandler((sp, msg) => GetRetryPolicy(sp))
    .AddPolicyHandler((sp, msg) => GetCircuitBreakerPolicy(sp))
    .AddPolicyHandler(GetTimeoutPolicy());
builder.Services.AddSingleton<IConnection>(provider =>
    RabbitMqExtensions.CreateRabbitMqConnectionAsync(
        rabbitMqSettings.HostName,
        rabbitMqSettings.UserName,
        rabbitMqSettings.Password,
        rabbitMqSettings.Port
    ).GetAwaiter().GetResult()
);

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddSingleton<IEventConsumer, RabbitMqEventConsumer>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudiences = jwtSettings.Audiences.Any() ? jwtSettings.Audiences : new List<string> { jwtSettings.Audience },
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "JWTWebAPIDemo", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter token as: Bearer {your token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});
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

// Middleware moved down

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
    dbContext.Database.Migrate();
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

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

// Handle Saga compensation (Seat Release)
var eventConsumer = app.Services.GetRequiredService<IEventConsumer>();
eventConsumer.Initialize("FlightService");
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

await eventConsumer.SubscribeAsync<BookingCancelledEvent>(async e =>
{
    if (e.ScheduleId.HasValue)
    {
        using var scope = scopeFactory.CreateScope();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IFlightScheduleService>();
        try 
        {
            await scheduleService.ReleaseScheduleSeatAsync(e.ScheduleId.Value, e.SeatClass, e.TicketCount);
            Log.Information($"Successfully released {e.TicketCount} {e.SeatClass} seats for Schedule {e.ScheduleId} via Saga compensation.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to release seats for Schedule {e.ScheduleId} during Saga compensation.");
        }
    }
});

await eventConsumer.StartAsync();

app.MapControllers();

app.Run();


