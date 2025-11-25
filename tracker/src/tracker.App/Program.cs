using System.Text;
using Akka.HealthCheck.Hosting;
using Akka.HealthCheck.Hosting.Web;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using tracker.App.Configuration;
using tracker.App.Services;
using tracker.Domain.MealPlan;

var builder = WebApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

/*
 * CONFIGURATION SOURCES
 */
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables();

// Initialize database if using PostgreSQL persistence
var akkaSettings = builder.Configuration.GetRequiredSection("AkkaSettings").Get<AkkaSettings>();
if (akkaSettings?.PersistenceMode == PersistenceMode.PostgreSql)
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSql");
    if (!string.IsNullOrEmpty(connectionString))
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("DatabaseInitializer");
        
        // Initialize Akka persistence tables
        await DatabaseInitializer.InitializeDatabase(connectionString, logger);
        
        // Add FluentMigrator services for application migrations
        builder.Services.AddDatabaseMigrations(connectionString);
        
        // Register MealPlanService
        builder.Services.AddSingleton<IMealPlanService>(sp => new MealPlanService(connectionString));
        
        // Register RecipeService
        builder.Services.AddSingleton<IRecipeService>(sp => new RecipeService(connectionString));
    }
}

// Add services to the container.
builder.Services.WithAkkaHealthCheck(HealthCheckType.All);
builder.Services.ConfigureWebApiAkka(builder.Configuration, (akkaConfigurationBuilder, serviceProvider) =>
{
    // we configure instrumentation separately from the internals of the ActorSystem
    akkaConfigurationBuilder.ConfigurePetabridgeCmd();
});

// Register JWT Service and Refresh Token Service
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IRefreshTokenService, RefreshTokenService>();

// Register Email Lookup Service (choose based on persistence mode)
if (akkaSettings?.PersistenceMode == PersistenceMode.PostgreSql)
{
    // Production: Use PostgreSQL for persistent email lookup
    var connectionString = builder.Configuration.GetConnectionString("PostgreSql");
    if (!string.IsNullOrEmpty(connectionString))
    {
        builder.Services.AddSingleton<IEmailLookupService>(sp => 
            new PostgreSqlEmailLookupService(
                connectionString, 
                sp.GetRequiredService<ILogger<PostgreSqlEmailLookupService>>()));
    }
    else
    {
        // Fallback to in-memory if no connection string
        builder.Services.AddSingleton<IEmailLookupService, EmailLookupService>();
    }
}
else
{
    // Development: Use in-memory (faster, simpler)
    // WARNING: All email mappings lost on restart!
    builder.Services.AddSingleton<IEmailLookupService, EmailLookupService>();
}

// Configure Memory Cache for rate limiting
builder.Services.AddMemoryCache();

// Configure IP Rate Limiting
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"] 
        ?? throw new InvalidOperationException("JWT Key is not configured");
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
    
    // For SignalR or other scenarios where the token might be in query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tracker API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5555", "http://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("*");
    });
});

var app = builder.Build();

// Run database migrations if PostgreSQL is configured
if (akkaSettings?.PersistenceMode == PersistenceMode.PostgreSql)
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSql");
    if (!string.IsNullOrEmpty(connectionString))
    {
        app.UseDatabaseMigrations();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Equals("Azure"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS before other middleware
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

// Enable IP Rate Limiting (must be before Authentication)
app.UseIpRateLimiting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapAkkaHealthCheckRoutes(optionConfigure: (_, opt) =>
{
    // Use a custom response writer to output a json of all reported statuses
    opt.ResponseWriter = Helper.JsonResponseWriter;
}); // needed for Akka.HealthCheck

app.MapControllers();

app.Run();