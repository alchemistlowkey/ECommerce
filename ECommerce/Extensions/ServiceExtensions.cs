using System;
using System.Text;
using Contracts;
using Entities.ConfigurationModels;
using Entities.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddHttpClient("Paystack");
        services.AddHttpClient("Flutterwave");

        // Register BOTH providers as concrete scoped types.
        // OrderService resolves the correct one at runtime based on the
        // user's checkout selection — no config switch needed.
        services.AddScoped<PaystackPaymentService>();
        services.AddScoped<FlutterwavePaymentService>();
    }

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

        var secret = jwtSection["Secret"];
        var issuer = jwtSection["validIssuer"];
        var audience = jwtSection["validAudience"];

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

    public static void ConfigurePaystack(
        this IServiceCollection services, IConfiguration configuration) =>
        services.Configure<PaystackSettings>(configuration.GetSection("Paystack"));

    public static void ConfigureFlutterwave(
        this IServiceCollection services, IConfiguration configuration) =>
        services.Configure<FlutterwaveSettings>(configuration.GetSection("Flutterwave"));
}