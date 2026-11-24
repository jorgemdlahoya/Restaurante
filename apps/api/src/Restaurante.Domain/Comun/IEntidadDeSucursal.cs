namespace Restaurante.Domain.Comun;

/// <summary>Entidad que pertenece a una sucursal de un restaurante (aislamiento por sucursal).</summary>
public interface IEntidadDeSucursal : IEntidadDeRestaurante
{
    Guid SucursalId { get; }
}
