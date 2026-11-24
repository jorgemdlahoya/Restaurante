using Restaurante.Application.Abstracciones;

namespace Restaurante.WebApi.Seguridad;

/// <summary>
/// Contexto de restaurante de la petición actual (scoped). Empieza en el ámbito de plataforma (<c>null</c>) y
/// lo fija <see cref="MiddlewareContextoRestaurante"/> antes de llegar a los controllers.
/// </summary>
public sealed class ContextoRestaurante : IContextoRestaurante
{
    public Guid? RestauranteId { get; private set; }

    public void Establecer(Guid restauranteId) => RestauranteId = restauranteId;
}
