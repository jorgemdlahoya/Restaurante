using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios.CambiarEstadoUsuario;

/// <summary>
/// Activa o desactiva (001/RF-15, 004/RF-05, RF-09, RF-10). Tras el validador (400): usuario de otro restaurante o
/// inexistente (404), alcance del actor (403) y, al desactivar, autoprotección y último administrador (409).
/// Desactivar renueva el sello (RF-13); reactivar no.
/// </summary>
public sealed class CambiarEstadoUsuarioHandler(IUsuarioRepository usuarios, ResolutorActor resolutor)
    : IRequestHandler<CambiarEstadoUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> Handle(CambiarEstadoUsuarioCommand request, CancellationToken cancellationToken)
    {
        var actor = await resolutor.ObtenerAsync(cancellationToken);
        var usuario = await usuarios.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Usuario no encontrado");
        if (!AlcanceGestionUsuarios.PuedeGestionar(actor, usuario))
        {
            throw new ProhibidoException(ResolutorActor.MensajeSinAlcance);
        }

        // El validador (pipeline) garantiza que Activo tiene valor.
        if (request.Activo is true)
        {
            usuario.Reactivar();
        }
        else
        {
            if (usuario.Id == actor.Id)
            {
                throw new ConflictoException("No puedes desactivar tu propio usuario");
            }

            if (usuario.Activo
                && usuario.Rol == Rol.AdministradorGeneral
                && await usuarios.ContarAdministradoresActivosAsync(cancellationToken) <= 1)
            {
                throw new ConflictoException("No se puede desactivar al último administrador activo");
            }

            usuario.Desactivar();
        }

        await usuarios.GuardarCambiosAsync(cancellationToken);
        return await resolutor.ADtoAsync(actor, usuario, cancellationToken);
    }
}
