using Akka.HealthCheck.Hosting;
using Akka.HealthCheck.Hosting.Web;
using tracker.App.Configuration;

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
    }
}

// Add services to the container.
builder.Services.WithAkkaHealthCheck(HealthCheckType.All);
builder.Services.ConfigureWebApiAkka(builder.Configuration, (akkaConfigurationBuilder, serviceProvider) =>
{
    // we configure instrumentation separately from the internals of the ActorSystem
    akkaConfigurationBuilder.ConfigurePetabridgeCmd();
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5555")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
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

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowFrontend");

app.MapAkkaHealthCheckRoutes(optionConfigure: (_, opt) =>
{
    // Use a custom response writer to output a json of all reported statuses
    opt.ResponseWriter = Helper.JsonResponseWriter;
}); // needed for Akka.HealthCheck
app.UseAuthorization();

app.MapControllers();

app.Run();