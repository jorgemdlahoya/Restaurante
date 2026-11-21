using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Application.Abstracciones;
using Restaurante.Infrastructure.Persistencia;
using Restaurante.Infrastructure.Seguridad;

namespace Restaurante.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, string cadenaConexion, string claveJwt)
    {
        services.AddDbContext<RestauranteDbContext>(o => o.UseNpgsql(cadenaConexion));
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<AplicadorMigraciones>();
        services.AddSingleton<IReloj, Reloj>();
        services.AddSingleton<IHasherContrasenas, HasherContrasenas>();
        services.AddSingleton<IEmisorTokens>(sp => new EmisorTokensJwt(claveJwt, sp.GetRequiredService<IReloj>()));
        return services;
    }
}
