using System.Text;
using AspNetCoreRateLimit;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TaskFlow.Presentation.Hubs;
using TaskFlow.Application;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Infrastructure;
using TaskFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog ----------
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// ---------- Layers ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IBoardNotifier, SignalRBoardNotifier>();

// ---------- Auth ----------
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    // Allows the SignalR client to authenticate via access_token query string,
    // since browser WebSocket clients can't set Authorization headers.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ---------- CORS ----------
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ---------- Rate limiting (protects auth endpoints from brute force) ----------
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ---------- Misc ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT}",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSignalR();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// ---------- Global exception handling ----------
// Converts FluentValidation errors -> 400, KeyNotFoundException -> 404,
// UnauthorizedAccessException -> 401, everything else -> 500 with no
// internal details leaked to the client.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var exception = feature?.Error;

        context.Response.ContentType = "application/json";

        int statusCode = 500;
        object body = new { title = "An unexpected error occurred" };

        if (exception is ValidationException vex)
        {
            statusCode = 400;
            body = new
            {
                title = "Validation failed",
                errors = vex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            };
        }
        else if (exception is KeyNotFoundException)
        {
            statusCode = 404;
            body = new { title = "Resource not found" };
        }
        else if (exception is UnauthorizedAccessException)
        {
            statusCode = 401;
            body = new { title = "Unauthorized" };
        }

        Log.Error(exception, "Unhandled exception processing {Path}", context.Request.Path);

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(body);
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();
app.UseIpRateLimiting();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BoardHub>("/hubs/board");
app.MapHealthChecks("/health");

// Auto-migrate in development for convenience; use proper migration
// deployment step (or `dotnet ef database update`) in production.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
