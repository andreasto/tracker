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
        
        // Register ProductService
        builder.Services.AddSingleton<IProductService>(sp => new ProductService(
            connectionString,
            sp.GetRequiredService<IProductApiClient>(),
            sp.GetRequiredService<ILogger<ProductService>>()));
    }
}

// Configure HttpClient for Product API
var productApiBaseUrl = builder.Configuration["ProductApi:BaseUrl"];
if (!string.IsNullOrEmpty(productApiBaseUrl))
{
    builder.Services.AddHttpClient<IProductApiClient, ProductApiClient>(client =>
    {
        client.BaseAddress = new Uri(productApiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        
        // Add any headers if needed (e.g., API key)
        var apiKey = builder.Configuration["ProductApi:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }
    });
}

// Add services to the container.
builder.Services.WithAkkaHealthCheck(HealthCheckType.All);
builder.Services.ConfigureWebApiAkka(builder.Configuration, (akkaConfigurationBuilder, serviceProvider) =>
{
    // we configure instrumentation separately from the internals of the ActorSystem
    akkaConfigurationBuilder.ConfigurePetabridgeCmd();
});

builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IRefreshTokenService, RefreshTokenService>();

if (akkaSettings?.PersistenceMode == PersistenceMode.PostgreSql)
{
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
        builder.Services.AddSingleton<IEmailLookupService, EmailLookupService>();
    }
}
else
{
    builder.Services.AddSingleton<IEmailLookupService, EmailLookupService>();
}

builder.Services.AddMemoryCache();

builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

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