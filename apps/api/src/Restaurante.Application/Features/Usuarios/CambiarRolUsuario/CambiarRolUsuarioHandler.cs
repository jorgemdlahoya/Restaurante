using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios.CambiarRolUsuario;

// Se sustituye por EditarUsuario en T-06; mientras tanto respeta el alcance de gestión (004/RF-09, RF-10).
public sealed class CambiarRolUsuarioHandler(IUsuarioRepository usuarios, ResolutorActor resolutor)
    : IRequestHandler<CambiarRolUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> Handle(CambiarRolUsuarioCommand request, CancellationToken cancellationToken)
    {
        var actor = await resolutor.ObtenerAsync(cancellationToken);
        var usuario = await usuarios.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Usuario no encontrado");

        var nuevo = RolExtensiones.DesdeTexto(request.Rol);
        if (!AlcanceGestionUsuarios.PuedeGestionar(actor, usuario)
            || !AlcanceGestionUsuarios.PuedeAsignar(actor, nuevo, usuario.Sucursales))
        {
            throw new ProhibidoException(ResolutorActor.MensajeSinAlcance);
        }

        if (usuario.Activo
            && usuario.Rol == Rol.AdministradorGeneral
            && nuevo != Rol.AdministradorGeneral
            && await usuarios.ContarAdministradoresActivosAsync(cancellationToken) <= 1)
        {
            throw new ConflictoException("No se puede quitar el rol al último administrador activo");
        }

        usuario.CambiarRol(nuevo);
        await usuarios.GuardarCambiosAsync(cancellationToken);
        return await resolutor.ADtoAsync(actor, usuario, cancellationToken);
    }
}
