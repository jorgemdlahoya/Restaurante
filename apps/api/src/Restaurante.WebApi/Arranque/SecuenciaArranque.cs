using Microsoft.Extensions.DependencyInjection;
using Restaurante.Infrastructure.Arranque;
using Restaurante.Infrastructure.Persistencia;

namespace Restaurante.WebApi.Arranque;

/// <summary>Tareas previas a servir peticiones: migraciones (si se piden) y administrador inicial.</summary>
public static class SecuenciaArranque
{
    /// <returns><c>false</c> si el arranque debe abortar; el motivo ya se registró en el log.</returns>
    public static async Task<bool> EjecutarAsync(
        IServiceProvider servicios,
        IReadOnlyDictionary<string, string?> entorno,
        bool aplicarMigraciones,
        ILogger logger,
        CancellationToken ct)
    {
        await using var ambito = servicios.CreateAsyncScope();

        await ambito.ServiceProvider.GetRequiredService<AplicadorMigraciones>()
            .AplicarAsync(aplicarMigraciones, ct);

        var resultado = await ambito.ServiceProvider.GetRequiredService<InicializadorAdministrador>()
            .InicializarAsync(entorno, ct);
        if (!resultado.Ok)
        {
            logger.LogCritical("No se pudo inicializar el administrador: {Error}", resultado.Error);
        }

        return resultado.Ok;
    }
}
