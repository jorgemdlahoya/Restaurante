using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios.EditarUsuario;

/// <summary>
/// Cambia rol y sucursales (004/RF-04, RF-09, RF-10). Tras el validador (400): usuario de otro restaurante o
/// inexistente (404), alcance del actor sobre el usuario y sobre lo que asigna (403; un slug inexistente pedido por
/// el general es 400), y las reglas de administrador (409, RF-05). Sólo guarda, y renueva el sello, si cambió el rol
/// o el conjunto de sucursales (RF-13).
/// </summary>
public sealed class EditarUsuarioHandler(IUsuarioRepository usuarios, ResolutorActor resolutor)
    : IRequestHandler<EditarUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> Handle(EditarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var actor = await resolutor.ObtenerAsync(cancellationToken);
        var usuario = await usuarios.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Usuario no encontrado");
        if (!AlcanceGestionUsuarios.PuedeGestionar(actor, usuario))
        {
            throw new ProhibidoException(ResolutorActor.MensajeSinAlcance);
        }

        var rol = RolExtensiones.DesdeTexto(request.Rol);
        var sucursales = await resolutor.ResolverSlugsAsync(actor, request.Sucursales, cancellationToken);
        if (!AlcanceGestionUsuarios.PuedeAsignar(actor, rol, sucursales))
        {
            throw new ProhibidoException(ResolutorActor.MensajeSinAlcance);
        }

        if (usuario.Rol == Rol.AdministradorGeneral && rol != Rol.AdministradorGeneral)
        {
            if (usuario.Id == actor.Id)
            {
                throw new ConflictoException("No puedes quitarte el rol de administrador general");
            }

            if (usuario.Activo && await usuarios.ContarAdministradoresActivosAsync(cancellationToken) <= 1)
            {
                throw new ConflictoException("No se puede quitar el rol al último administrador activo");
            }
        }

        if (usuario.AsignarRolYSucursales(rol, sucursales))
        {
            await usuarios.GuardarCambiosAsync(cancellationToken);
        }

        return await resolutor.ADtoAsync(actor, usuario, cancellationToken);
    }
}
