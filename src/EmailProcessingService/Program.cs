using EmailProcessingService.Services;
using EmailProcessingService.Models;
using EmailProcessingService.Data;
using EmailProcessingService.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs explicitly
// builder.WebHost.UseUrls("https://localhost:7000", "http://localhost:5000");

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add Entity Framework
builder.Services.AddDbContext<EmailProcessingDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (connectionString == "InMemory")
    {
        options.UseInMemoryDatabase("EmailProcessingDb");
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

// Configure blockchain settings
builder.Services.Configure<BlockchainConfiguration>(builder.Configuration.GetSection("Blockchain"));

// Add core services - PRODUCTION GRADE
builder.Services.AddScoped<IEmailParserService, EmailParserService>();
builder.Services.AddScoped<IEmailValidationService, EmailValidationService>();
builder.Services.AddScoped<IWalletCreatorService, WalletCreatorService>();
builder.Services.AddScoped<EmailProcessingService.Services.IEmailProcessingService, EmailProcessingService.Services.EmailProcessingService>();

// Production blockchain services
builder.Services.AddSingleton<EmailProcessingService.Contracts.IContractAbiService, EmailProcessingService.Contracts.ContractAbiService>();
builder.Services.AddScoped<IProductionBlockchainService, ProductionBlockchainService>();

// Test blockchain service - ENABLED for testing
builder.Services.AddScoped<IBlockchainService, BlockchainService>();

// Add IPFS services - NEW
builder.Services.AddScoped<IIpfsService, IpfsService>();
builder.Services.AddScoped<IIpfsValidationService, IpfsValidationService>();

// Add MVP implementations - Repository
builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
// For production, uncomment and use: builder.Services.AddScoped<ITaskRepository, TaskRepository>();

// Add MVP implementations - Authorization & Notifications
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<INotificationService, SimpleNotificationService>();

// Add existing placeholder implementations
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<IWhitelistService, WhitelistService>();
builder.Services.AddScoped<IFileProcessorService, FileProcessorService>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add authentication and authorization
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
        };
    });

builder.Services.AddAuthorization();

// Add CORS - Enhanced for IPFS support and Swagger
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins(
                "https://auth.rootz.global", 
                "https://wallet.rootz.global",
                "https://ipfs.rootz.global",
                "https://rootz.global",     // Main website HTTPS
                "http://rootz.global",      // Main website HTTP
                "https://localhost:7000",  // Add for Swagger HTTPS
                "http://localhost:5000",   // Add for Swagger HTTP
                "http://localhost:3000")   // For development
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Add health checks - Enhanced
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EmailProcessingDbContext>()
    .AddTypeActivatedCheck<IpfsHealthCheck>("ipfs");

// Add HTTP client - Enhanced for IPFS
builder.Services.AddHttpClient<IIpfsService, IpfsService>(client =>
{
    // Configure based on IPFS settings
    var ipfsConfig = builder.Configuration.GetSection("IPFS");
    client.Timeout = TimeSpan.FromMinutes(ipfsConfig.GetValue("TimeoutMinutes", 5));
    client.DefaultRequestHeaders.Add("User-Agent", "EmailDataWallet/1.0");
});

// Add Swagger/OpenAPI - ALWAYS ENABLED FOR TESTING
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Email Data Wallet Processing Service with IPFS",
        Version = "v2.0",
        Description = "API for processing emails into blockchain-verified data wallets stored on IPFS",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Rootz Support",
            Email = "support@rootz.global",
            Url = new Uri("https://rootz.global")
        }
    });
    
    // Add multiple server configurations for testing
    c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer
    {
    Url = "https://rootz.global:7000",
    Description = "HTTPS endpoint"
    });

    c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer
    {
    Url = "http://rootz.global:5000",
    Description = "HTTP endpoint"
    });
    
    // Add XML documentation if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline - SWAGGER ALWAYS ENABLED
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Email Data Wallet API v2.0");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Email Data Wallet Service API";
    c.DisplayRequestDuration();
    c.EnableTryItOutByDefault();
    c.DefaultModelsExpandDepth(1);
});

// Enable developer exception page for better error details
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

}

// app.UseHttpsRedirection(); // Disabled for mixed content compatibility

// Enable static files for web interfaces
// Configure default files first
var defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("index.html");
app.UseDefaultFiles(defaultFilesOptions);
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Add IPFS-specific health check endpoint
app.MapHealthChecks("/health/ipfs", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == "ipfs"
});

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EmailProcessingDbContext>();
    context.Database.EnsureCreated();
    
    // Test IPFS connectivity on startup
    try
    {
        var ipfsService = scope.ServiceProvider.GetRequiredService<IIpfsService>();
        var testResult = await ipfsService.UploadFileAsync(
            System.Text.Encoding.UTF8.GetBytes("startup-test"),
            "startup-test.txt",
            new Dictionary<string, object> { { "startup", true } });
            
        if (testResult.Success)
        {
            app.Logger.LogInformation("IPFS connectivity verified: {Hash}", testResult.IpfsHash);
        }
        else
        {
            app.Logger.LogWarning("IPFS connectivity test failed: {Error}", testResult.ErrorMessage);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to test IPFS connectivity on startup");
    }
}

// Enhanced startup logging with Swagger info
app.Logger.LogInformation("=== Email Processing Service with IPFS Starting ===");
app.Logger.LogInformation("API available at: https://localhost:7000 and http://localhost:5000");
app.Logger.LogInformation("Swagger UI: https://localhost:7000/swagger");
app.Logger.LogInformation("Swagger JSON: https://localhost:7000/swagger/v1/swagger.json");
app.Logger.LogInformation("Health check: https://localhost:7000/health");
app.Logger.LogInformation("IPFS Health check: https://localhost:7000/health/ipfs");
app.Logger.LogInformation("Feature: IPFS Storage Integration - ENABLED");
app.Logger.LogInformation("Feature: Blockchain Verification - ENABLED");
app.Logger.LogInformation("Feature: Enhanced Email Processing - ENABLED");
app.Logger.LogInformation("Feature: Swagger Documentation - ALWAYS ENABLED");
app.Logger.LogInformation("===========================================");

app.Run();
