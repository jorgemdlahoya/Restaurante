namespace Restaurante.Application.Abstracciones;

/// <summary>Usuario autenticado que ejecuta la operación en curso.</summary>
public interface IUsuarioActual
{
    Guid Id { get; }
}
