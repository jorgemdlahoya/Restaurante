namespace Restaurante.Domain.Comun;

/// <summary>Entidad que pertenece a un restaurante (aislamiento por restaurante).</summary>
public interface IEntidadDeRestaurante
{
    Guid RestauranteId { get; }
}
