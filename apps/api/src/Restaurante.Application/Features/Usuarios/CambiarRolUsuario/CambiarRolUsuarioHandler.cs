using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios.CambiarRolUsuario;

public sealed class CambiarRolUsuarioHandler(IUsuarioRepository usuarios)
    : IRequestHandler<CambiarRolUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> Handle(CambiarRolUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Usuario no encontrado");

        var nuevo = Enum.Parse<Rol>(request.Rol, ignoreCase: true);

        if (usuario.Activo
            && usuario.Rol == Rol.Administrador
            && nuevo != Rol.Administrador
            && await usuarios.ContarAdministradoresActivosAsync(cancellationToken) <= 1)
        {
            throw new ConflictoException("No se puede quitar el rol al último administrador activo");
        }

        usuario.CambiarRol(nuevo);
        await usuarios.GuardarCambiosAsync(cancellationToken);
        return UsuarioDto.Desde(usuario);
    }
}
