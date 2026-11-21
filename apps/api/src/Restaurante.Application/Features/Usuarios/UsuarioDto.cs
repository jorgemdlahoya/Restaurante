using Restaurante.Application.Comun;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios;

public sealed record UsuarioDto(Guid Id, string NombreUsuario, string Rol, bool Activo)
{
    public static UsuarioDto Desde(Usuario usuario) =>
        new(usuario.Id, usuario.NombreUsuario, usuario.Rol.ComoTexto(), usuario.Activo);
}
