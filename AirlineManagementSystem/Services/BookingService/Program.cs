using Shared.Middleware;
using Serilog;
using Serilog.Context;
using BookingService.CQRS.Handlers;
using BookingService.Data;
using BookingService.Repositories;
using BookingService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using Shared.Configuration;
using Shared.Events;
using Shared.RabbitMQ;

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

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPassengerRepository, PassengerRepository>();
builder.Services.AddScoped<IBookingService, BookingServiceImpl>();
builder.Services.AddScoped<IPassengerService, PassengerService>();

// Register CQRS Handlers
builder.Services.AddScoped<CreateBookingCommandHandler>();
builder.Services.AddScoped<CancelBookingCommandHandler>();
builder.Services.AddScoped<CreatePassengerCommandHandler>();
builder.Services.AddScoped<CancelPassengerCommandHandler>();
builder.Services.AddScoped<HandlePaymentSuccessCommandHandler>();
builder.Services.AddScoped<HandlePaymentFailedCommandHandler>();
builder.Services.AddScoped<GetBookingByIdQueryHandler>();
builder.Services.AddScoped<GetBookingHistoryQueryHandler>();
builder.Services.AddScoped<GetBookingsByScheduleQueryHandler>();
builder.Services.AddScoped<GetOccupiedSeatsQueryHandler>();
builder.Services.AddScoped<GetPassengersForBookingQueryHandler>();
builder.Services.AddScoped<GetBookingByPnrQueryHandler>();

builder.Services.AddHttpClient<BookingServiceImpl>();
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
            ValidAudience = jwtSettings.Audience,
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
app.UseMiddleware<GlobalExceptionMiddleware>();

// Middleware moved down

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    dbContext.Database.Migrate();
}

// RabbitMQ consumers are singleton and run for the lifetime of the app.
// Each handler must create its own scope to resolve scoped services (DbContext, repositories).
var eventConsumer = app.Services.GetRequiredService<IEventConsumer>();
eventConsumer.Initialize("BookingService");
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

await eventConsumer.SubscribeAsync<PaymentSuccessEvent>(async e =>
{
    using var handlerScope = scopeFactory.CreateScope();
    var handler = handlerScope.ServiceProvider.GetRequiredService<HandlePaymentSuccessCommandHandler>();
    var command = new BookingService.CQRS.Commands.HandlePaymentSuccessCommand(e);
    await handler.HandleAsync(command);
});

await eventConsumer.SubscribeAsync<PaymentFailedEvent>(async e =>
{
    using var handlerScope = scopeFactory.CreateScope();
    var handler = handlerScope.ServiceProvider.GetRequiredService<HandlePaymentFailedCommandHandler>();
    var command = new BookingService.CQRS.Commands.HandlePaymentFailedCommand(e);
    await handler.HandleAsync(command);
});

await eventConsumer.StartAsync();

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
app.MapControllers();

app.Run();


