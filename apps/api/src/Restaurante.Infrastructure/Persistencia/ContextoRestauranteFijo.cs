using Restaurante.Application.Abstracciones;

namespace Restaurante.Infrastructure.Persistencia;

/// <summary>Contexto de restaurante con un valor fijo (fábrica de diseño, pruebas y registro provisional).</summary>
public sealed class ContextoRestauranteFijo(Guid? restauranteId) : IContextoRestaurante
{
    public Guid? RestauranteId { get; } = restauranteId;
}
