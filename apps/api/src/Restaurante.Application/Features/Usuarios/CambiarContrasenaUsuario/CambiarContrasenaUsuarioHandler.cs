using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Usuarios.CambiarContrasenaUsuario;

/// <summary>
/// Asigna una contraseña nueva (004/RF-06, RF-10, RF-13). Tras el validador (400): usuario de otro restaurante o
/// inexistente (404) y alcance del actor (403). El administrador general puede cambiar la suya; el de sucursal
/// nunca a sí mismo (lo decide <see cref="AlcanceGestionUsuarios"/>). Guarda sólo el hash y renueva el sello.
/// </summary>
public sealed class CambiarContrasenaUsuarioHandler(
    IUsuarioRepository usuarios, ResolutorActor resolutor, IHasherContrasenas hasher)
    : IRequestHandler<CambiarContrasenaUsuarioCommand, Unit>
{
    public async Task<Unit> Handle(CambiarContrasenaUsuarioCommand request, CancellationToken cancellationToken)
    {
        var actor = await resolutor.ObtenerAsync(cancellationToken);
        var usuario = await usuarios.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Usuario no encontrado");
        if (!AlcanceGestionUsuarios.PuedeGestionar(actor, usuario))
        {
            throw new ProhibidoException(ResolutorActor.MensajeSinAlcance);
        }

        usuario.CambiarContrasena(hasher.Hashear(request.Contrasena));
        await usuarios.GuardarCambiosAsync(cancellationToken);
        return Unit.Value;
    }
}
