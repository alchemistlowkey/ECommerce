using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ECommerce;
using ECommerce.Extensions;
using ECommerce.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

// ── Bootstrap logger (captures startup errors before DI is ready) ─────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ECommerce API");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog (reads full config from appsettings) ──────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
    );

    // ── Infrastructure ────────────────────────────────────────────────────────
    builder.Services.ConfigureCors();
    builder.Services.ConfigurePostgreSqlContext(builder.Configuration);

    // ── Identity (must come after DbContext, before JWT) ──────────────────────
    // Registers UserManager<User>, password hashing, AspNetUsers table, etc.
    builder.Services.ConfigureIdentity();

    // ── Application Services ──────────────────────────────────────────────────
    builder.Services.ConfigureRepositoryManager();
    builder.Services.ConfigureServiceManager();
    builder.Services.ConfigurePaymentService(builder.Configuration);

    // ── Auth & Security ───────────────────────────────────────────────────────
    builder.Services.ConfigureJwt(builder.Configuration);
    builder.Services.ConfigurePaystack(builder.Configuration);
    builder.Services.ConfigureFlutterwave(builder.Configuration);

    // ── AutoMapper ────────────────────────────────────────────────────────────
    builder.Services.AddAutoMapper(typeof(MappingProfile));


    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    // ── OpenAPI with Bearer security scheme ──────────────────────────────────
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["BearerAuth"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter your JWT token"
                }
            };
            return Task.CompletedTask;
        });
    });

    // ── Controllers (discovered from ECommerce.Presentation assembly) ─────────
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(ECommerce.Presentation.AssemblyReference).Assembly)
        .AddJsonOptions(options =>
        {
            // CamelCase policy ensures { "productId": ... } binds to ProductId on all record types.
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    // ── Swagger ───────────────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();

    // ── Authorization ─────────────────────────────────────────────────────────
    builder.Services.AddAuthorization();

    var app = builder.Build();

    // ── Middleware Pipeline ───────────────────────────────────────────────────
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // Serilog structured HTTP request logging — replaces the default
    // Microsoft request logs with one clean line per request.
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
            if (httpContext.User.Identity?.IsAuthenticated == true)
                diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
        };
    });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {

    }

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("ECommerce API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
            .AddPreferredSecuritySchemes("BearerAuth")
            .AddHttpAuthentication("BearerAuth", auth =>
            {
                auth.Token = "";
            });
    });

    app.UseHttpsRedirection();
    app.UseCors("CorsPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // ── Seed admin user on first run ──────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        await SeedData.EnsureAdminAsync(scope.ServiceProvider);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ECommerce API terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;