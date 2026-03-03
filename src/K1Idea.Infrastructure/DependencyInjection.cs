using Dapper;
using K1Idea.Application.Common.Interfaces;
using K1Idea.Domain.Interfaces;
using K1Idea.Infrastructure.Auth;
using K1Idea.Infrastructure.Data;
using K1Idea.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace K1Idea.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Map snake_case DB columns to PascalCase C# properties
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

        var jwtSettings = new JwtSettings
        {
            Secret = configuration["JWT:Secret"]
                ?? throw new InvalidOperationException("JWT:Secret is required."),
            Issuer = configuration["JWT:Issuer"]
                ?? throw new InvalidOperationException("JWT:Issuer is required."),
            Audience = configuration["JWT:Audience"]
                ?? throw new InvalidOperationException("JWT:Audience is required."),
        };

        services.AddSingleton(new NpgsqlConnectionFactory(connectionString));
        services.AddSingleton(jwtSettings);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrgRepository, OrgRepository>();
        services.AddScoped<IBusinessUnitRepository, BusinessUnitRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
