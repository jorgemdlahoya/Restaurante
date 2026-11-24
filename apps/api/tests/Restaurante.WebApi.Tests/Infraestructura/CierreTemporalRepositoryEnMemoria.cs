using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;

namespace Restaurante.WebApi.Tests.Infraestructura;

/// <summary>Reloj de prueba con un instante fijo.</summary>
public sealed class RelojFijo(DateTimeOffset ahora) : IReloj
{
    public DateTimeOffset Ahora { get; set; } = ahora;
}

/// <summary>Almacén de cierres compartido por todas las peticiones de una <see cref="FabricaApi"/>, sin filtrar.</summary>
public sealed class AlmacenCierres
{
    public List<CierreTemporal> Cierres { get; } = new();
}

/// <summary>
/// Repositorio por petición que emula el filtro global de EF: sólo cierres del restaurante y la sucursal del
/// contexto de la petición, que el middleware fija después de crear el ámbito (por eso se leen en cada consulta).
/// </summary>
public sealed class CierreTemporalRepositoryEnMemoria(
    AlmacenCierres almacen, IContextoRestaurante contexto, IContextoSucursal contextoSucursal) : ICierreTemporalRepository
{
    private IEnumerable<CierreTemporal> Visibles => almacen.Cierres
        .Where(c => c.RestauranteId == contexto.RestauranteId && c.SucursalId == contextoSucursal.SucursalId);

    public Task<IReadOnlyList<CierreTemporal>> ListarDesdeAsync(DateTimeOffset desde, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CierreTemporal>>(Visibles.Where(c => c.Fin > desde).OrderBy(c => c.Inicio).ToList());

    public Task<CierreTemporal?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(c => c.Id == id));

    public void Agregar(CierreTemporal cierre) => almacen.Cierres.Add(cierre);

    public Task GuardarCambiosAsync(CancellationToken ct) => Task.CompletedTask;
}
