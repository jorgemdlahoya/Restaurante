using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios.CambiarEstadoUsuario;

public sealed class CambiarEstadoUsuarioHandler(
    IUsuarioRepository usuarios,
    IUsuarioActual usuarioActual) : IRequestHandler<CambiarEstadoUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> Handle(CambiarEstadoUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Usuario no encontrado");

        // El validador (pipeline) garantiza que Activo tiene valor.
        if (request.Activo is true)
        {
            usuario.Reactivar();
        }
        else
        {
            if (usuario.Id == usuarioActual.Id)
            {
                throw new ConflictoException("No puedes desactivar tu propio usuario");
            }

            if (usuario.Activo
                && usuario.Rol == Rol.Administrador
                && await usuarios.ContarAdministradoresActivosAsync(cancellationToken) <= 1)
            {
                throw new ConflictoException("No se puede desactivar al último administrador activo");
            }

            usuario.Desactivar();
        }

        await usuarios.GuardarCambiosAsync(cancellationToken);
        return UsuarioDto.Desde(usuario);
    }
}
