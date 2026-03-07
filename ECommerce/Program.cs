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

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure ───────────────────────────────────────────────────────────
builder.Services.ConfigureCors();
builder.Services.ConfigurePostgreSqlContext(builder.Configuration);

// ── Identity (must come after DbContext, before JWT) ─────────────────────────
// Registers UserManager<User>, password hashing, AspNetUsers table, etc.
builder.Services.ConfigureIdentity();

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.ConfigureRepositoryManager();
builder.Services.ConfigureServiceManager();
builder.Services.ConfigurePaymentService(builder.Configuration);

// ── Auth & Security ───────────────────────────────────────────────────────────
builder.Services.ConfigureJwt(builder.Configuration);
builder.Services.ConfigureStripe(builder.Configuration);
builder.Services.ConfigurePaystack(builder.Configuration);

// ── AutoMapper ────────────────────────────────────────────────────────────────
builder.Services.AddAutoMapper(typeof(MappingProfile));


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// ── OpenAPI with Bearer security scheme ──────────────────────────────────────
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

// ── Controllers (discovered from ECommerce.Presentation assembly) ─────────────
builder.Services.AddControllers()
    .AddApplicationPart(typeof(ECommerce.Presentation.AssemblyReference).Assembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();

// ── Authorization ─────────────────────────────────────────────────────────────
builder.Services.AddAuthorization();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

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

// ── Seed admin user on first run ──────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    await SeedData.EnsureAdminAsync(scope.ServiceProvider);
}

app.Run();