using Microsoft.EntityFrameworkCore;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Infrastructure.Persistencia;

/// <summary>Las consultas pasan por el filtro global: sólo cierres de la sucursal del contexto.</summary>
public sealed class CierreTemporalRepository(RestauranteDbContext contexto) : ICierreTemporalRepository
{
    public async Task<IReadOnlyList<CierreTemporal>> ListarDesdeAsync(DateTimeOffset desde, CancellationToken ct) =>
        await contexto.CierresTemporales.Where(c => c.Fin > desde).OrderBy(c => c.Inicio).ToListAsync(ct);

    public Task<CierreTemporal?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        contexto.CierresTemporales.FirstOrDefaultAsync(c => c.Id == id, ct);

    public void Agregar(CierreTemporal cierre) => contexto.CierresTemporales.Add(cierre);

    public Task GuardarCambiosAsync(CancellationToken ct) => contexto.SaveChangesAsync(ct);
}
