namespace Restaurante.Application.Abstracciones;

/// <summary>
/// Restaurante en cuyo ámbito se atiende la petición. <c>null</c> es el ámbito de plataforma:
/// sólo se ven los usuarios sin restaurante (administradores de plataforma).
/// </summary>
public interface IContextoRestaurante
{
    Guid? RestauranteId { get; }
}
