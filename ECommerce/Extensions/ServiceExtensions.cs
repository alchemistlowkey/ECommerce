using System;
using System.Text;
using Contracts;
using Entities.ConfigurationModels;
using Entities.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Repository;
using Service;
using Service.Contracts;

namespace ECommerce.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureCors(this IServiceCollection services) =>
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
        });

    public static void ConfigureRepositoryManager(this IServiceCollection services) =>
        services.AddScoped<IRepositoryManager, RepositoryManager>();

    public static void ConfigureServiceManager(this IServiceCollection services) =>
        services.AddScoped<IServiceManager, ServiceManager>();

    public static void ConfigurePaymentService(this IServiceCollection services, IConfiguration configuration)
    {
        // Register HttpClient for Paystack (no-op cost if Stripe is selected)
        services.AddHttpClient("Paystack");

        var provider = configuration["PaymentProvider"] ?? "Stripe";

        // The active provider is registered as IPaymentService.
        // To switch providers: change "PaymentProvider" in appsettings.json.
        // No code changes required.
        if (provider.Equals("Paystack", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IPaymentService, PaystackPaymentService>();
        else
            services.AddScoped<IPaymentService, StripePaymentService>();
    }

    public static void ConfigureSqlContext(this IServiceCollection services, IConfiguration configuration) =>
        services.AddDbContext<RepositoryContext>(opts =>
        opts.UseSqlServer(configuration.GetConnectionString("sqlConnection")));

    public static void ConfigurePostgreSqlContext(this IServiceCollection services, IConfiguration configuration) =>
        services.AddDbContext<RepositoryContext>(opts =>
        opts.UseNpgsql(configuration.GetConnectionString("sqlConnection"),
        b => b.MigrationsAssembly("Repository")));

    public static void ConfigureIdentity(this IServiceCollection services) =>
        services.AddIdentity<User, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<RepositoryContext>()
        .AddDefaultTokenProviders();

    public static void ConfigureJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("JwtSettings");
        services.Configure<JwtConfiguration>(jwtSection);

        // Read values directly from IConfiguration — never depends on Get<T>()
        // which silently returns null properties when a key is absent, causing a
        // cryptic ArgumentNullException inside the JWT middleware on the first request.
        var secret = jwtSection["Secret"];
        var issuer = jwtSection["validIssuer"];
        var audience = jwtSection["validAudience"];

        // Fail at startup with a clear message rather than at request time.
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                "JWT Secret is missing. Set 'JwtSettings:Secret' in appsettings.json.");

        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException(
                "JWT Issuer is missing. Set 'JwtSettings:validIssuer' in appsettings.json.");

        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException(
                "JWT Audience is missing. Set 'JwtSettings:validAudience' in appsettings.json.");

        services.AddAuthentication(options =>
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
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secret))
            };
        });
    }

    public static void ConfigureStripe(
        this IServiceCollection services, IConfiguration configuration) =>
        services.Configure<StripeSettings>(configuration.GetSection("Stripe"));

    public static void ConfigurePaystack(
        this IServiceCollection services, IConfiguration configuration) =>
        services.Configure<PaystackSettings>(configuration.GetSection("Paystack"));

    public static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(s =>
        {
            s.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ECommerce API",
                Version = "v1",
                Description = "ECommerce API by Alchemistlowkey",
                TermsOfService = new Uri("https://example.com/terms"),
                Contact = new OpenApiContact
                {
                    Name = "Lucky Samuel",
                    Email = "alchemistlowkey@gmail.com",
                    Url = new Uri("https://x.com/alchemistlowkey"),
                },
                License = new OpenApiLicense
                {
                    Name = "ECommerce API LICX",
                    Url = new Uri("https://example.com/license"),
                }
            });

            var xmlFile = $"{typeof(Presentation.AssemblyReference).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            s.IncludeXmlComments(xmlPath);

            s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Place to add JWT with Bearer",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            s.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });
    }
}
