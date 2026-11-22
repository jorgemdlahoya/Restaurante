using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Restaurante.Application.Abstracciones;
using Restaurante.Infrastructure.Arranque;
using Restaurante.Infrastructure.Persistencia;
using Restaurante.Infrastructure.Seguridad;

namespace Restaurante.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, string cadenaConexion, string claveJwt)
    {
        // Reintenta fallos transitorios de PostgreSQL (p. ej. la base aún arrancando). La estrategia de
        // reintento no admite transacciones iniciadas por el usuario salvo dentro de
        // CreateExecutionStrategy().Execute(...); hoy no hay ninguna (SaveChanges ya es atómico).
        services.AddDbContext<RestauranteDbContext>(o =>
            o.UseNpgsql(cadenaConexion, npgsql => npgsql.EnableRetryOnFailure()));
        // Provisional hasta T-07 (middleware de restaurante): ámbito de plataforma por defecto.
        services.TryAddScoped<IContextoRestaurante>(_ => new ContextoRestauranteFijo(null));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRestauranteRepository, RestauranteRepository>();
        services.AddScoped<AplicadorMigraciones>();
        services.AddScoped<InicializadorAdministrador>();
        services.AddSingleton<IReloj, Reloj>();
        services.AddSingleton<IHasherContrasenas, HasherContrasenas>();
        services.AddSingleton<IEmisorTokens>(sp => new EmisorTokensJwt(claveJwt, sp.GetRequiredService<IReloj>()));
        return services;
    }
}
