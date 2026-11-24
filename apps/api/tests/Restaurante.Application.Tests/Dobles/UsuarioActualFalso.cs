using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Dobles;

public sealed class UsuarioActualFalso(
    Guid id, Rol rol = Rol.AdministradorGeneral, IReadOnlyCollection<Guid>? sucursales = null) : IUsuarioActual
{
    public Guid Id { get; } = id;

    public Rol Rol { get; } = rol;

    public IReadOnlyCollection<Guid> Sucursales { get; } = sucursales ?? [];
}
