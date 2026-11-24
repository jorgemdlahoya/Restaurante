using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Tests.Dobles;

/// <summary>
/// Repositorio en memoria que emula el filtro global de EF: sólo cierres de la sucursal del contexto.
/// <see cref="Cierres"/> expone el almacén completo.
/// </summary>
public sealed class CierreTemporalRepositoryEnMemoria(IContextoSucursal contexto) : ICierreTemporalRepository
{
    public List<CierreTemporal> Cierres { get; } = new();

    private IEnumerable<CierreTemporal> Visibles => Cierres.Where(c => c.SucursalId == contexto.SucursalId);

    public Task<IReadOnlyList<CierreTemporal>> ListarDesdeAsync(DateTimeOffset desde, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CierreTemporal>>(Visibles.Where(c => c.Fin > desde).OrderBy(c => c.Inicio).ToList());

    public Task<CierreTemporal?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(c => c.Id == id));

    public void Agregar(CierreTemporal cierre) => Cierres.Add(cierre);

    public Task GuardarCambiosAsync(CancellationToken ct) => Task.CompletedTask;
}
