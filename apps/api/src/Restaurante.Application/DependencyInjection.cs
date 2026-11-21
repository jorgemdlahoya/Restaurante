using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Application.Comun.Behaviors;

namespace Restaurante.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var ensamblado = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(ensamblado);
            // El orden de registro es el orden de ejecución: validación primero, luego logging.
            cfg.AddOpenBehavior(typeof(ValidacionBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });
        services.AddValidatorsFromAssembly(ensamblado);

        return services;
    }
}
