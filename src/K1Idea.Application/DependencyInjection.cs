using FluentValidation;
using K1Idea.Application.Common.Behaviors;
using K1Idea.Application.Common.Tenancy;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace K1Idea.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<TenantContext>();
        services.AddScoped<OrgContext>();

        return services;
    }
}
