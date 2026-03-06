using System.Text.Json.Serialization;
using ECommerce;
using ECommerce.Extensions;
using ECommerce.Middleware;

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
builder.Services.AddOpenApi();

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