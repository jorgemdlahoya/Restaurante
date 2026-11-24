using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Tests.Dobles;

/// <summary>Contexto de restaurante fijo para las pruebas; <c>null</c> es el ámbito de plataforma.</summary>
public sealed class ContextoRestauranteFalso(Guid? restauranteId) : IContextoRestaurante
{
    public Guid? RestauranteId { get; } = restauranteId;
}
