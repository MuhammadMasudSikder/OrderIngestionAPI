using Application.Commands.Orders;
using FluentValidation;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OrderIngestionAPI.Middleware;
using OrderIngestionAPI.Validators;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Application Insights (Monitoring) Telemetry
builder.Services.AddApplicationInsightsTelemetry();

////Logging External Calls (HTTP, Polly)
//builder.Services.AddHttpClient<ILogisticsClient>()
//    .AddPolicyHandler(Policy
//        .Handle<HttpRequestException>()
//        .RetryAsync(3, (ex, retry) =>
//        {
//            logger.LogWarning(ex,
//                "Retry {Retry} calling logistics service",
//                retry);
//        }));

//builder.WebHost.UseUrls("http://+:8080");
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(IngestOrderCommand).Assembly);
});

builder.Services.AddValidatorsFromAssembly(typeof(IngestOrderCommand).Assembly);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString);

//Configure Serilog
Directory.CreateDirectory(
    Path.Combine(AppContext.BaseDirectory, "Logs")
);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug() // logs Information, Debug, Error, etc.
    .WriteTo.Console()    // optional: console logging
    .WriteTo.File(
        path: "Logs/order-ingestion-.log", // log file path
        rollingInterval: RollingInterval.Day, // create a new file daily
        retainedFileCountLimit: 30, // keep last 30 log files
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// Register Layers
//builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT configuration from appsettings.json
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

// Register JWT authentication middleware
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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Information("Authentication Failed: " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Log.Information("Token Validated Successfully!");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddValidatorsFromAssemblyContaining<OrderIngestRequestValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();


// Middlewares order matters
// JWT authentication middleware runs here
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderIngestion API v1");
    });
}

// Landing page at root URL
app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(@"
        <html>
            <head><title>OrderIngestion API</title></head>
            <body>
                <h1>OrderIngestion API is running</h1>
                <p>Use <a href='/health'>/health</a> to check status.</p>
            </body>
        </html>
    ");
});

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok("Healthy"));
app.MapHealthChecks("/health");
app.MapControllers();

//app.Run();bbb

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
