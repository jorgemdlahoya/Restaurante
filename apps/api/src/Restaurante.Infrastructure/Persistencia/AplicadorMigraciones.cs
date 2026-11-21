using Microsoft.EntityFrameworkCore;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class AplicadorMigraciones(RestauranteDbContext contexto)
{
    public async Task AplicarAsync(bool aplicar, CancellationToken ct)
    {
        if (!aplicar)
        {
            return;
        }

        await contexto.Database.MigrateAsync(ct);
    }
}
