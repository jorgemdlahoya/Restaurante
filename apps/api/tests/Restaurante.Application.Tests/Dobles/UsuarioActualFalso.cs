using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Tests.Dobles;

public sealed class UsuarioActualFalso(Guid id) : IUsuarioActual
{
    public Guid Id { get; } = id;
}
