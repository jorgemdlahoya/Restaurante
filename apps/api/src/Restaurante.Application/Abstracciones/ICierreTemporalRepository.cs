using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Abstracciones;

/// <summary>Cierres temporales de la sucursal del contexto (filtro global).</summary>
public interface ICierreTemporalRepository
{
    /// <summary>Cierres con <c>Fin</c> posterior a <paramref name="desde"/>, ordenados por inicio.</summary>
    Task<IReadOnlyList<CierreTemporal>> ListarDesdeAsync(DateTimeOffset desde, CancellationToken ct);

    Task<CierreTemporal?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    void Agregar(CierreTemporal cierre);

    Task GuardarCambiosAsync(CancellationToken ct);
}
