using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios.CrearUsuario;

/// <summary>
/// Alta de usuario con sucursales (004/RF-01, RF-02, RF-08). Tras el validador (400), comprueba el alcance (403:
/// el administrador de sucursal sólo da de alta cajeros y cocineros de su sucursal), resuelve los slugs (400 si
/// alguno no existe) y por último el nombre duplicado (409).
/// </summary>
public sealed class CrearUsuarioHandler(
    IUsuarioRepository usuarios,
    IHasherContrasenas hasher,
    IReloj reloj,
    IContextoRestaurante contexto,
    ResolutorActor resolutor) : IRequestHandler<CrearUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> Handle(CrearUsuarioCommand request, CancellationToken cancellationToken)
    {
        var restauranteId = contexto.RestauranteId
            ?? throw new InvalidOperationException("No se puede crear un usuario fuera del ámbito de un restaurante.");

        var actor = await resolutor.ObtenerAsync(cancellationToken);
        var rol = RolExtensiones.DesdeTexto(request.Rol);
        var sucursales = await resolutor.ResolverSlugsAsync(actor, request.Sucursales, cancellationToken);
        if (!AlcanceGestionUsuarios.PuedeAsignar(actor, rol, sucursales))
        {
            throw new ProhibidoException(ResolutorActor.MensajeSinAlcance);
        }

        var nombre = request.NombreUsuario.Trim();
        var existente = await usuarios.ObtenerPorNombreNormalizadoAsync(nombre.ToLowerInvariant(), cancellationToken);
        if (existente is not null)
        {
            throw new ConflictoException("Ya existe un usuario con ese nombre");
        }

        var usuario = Usuario.Crear(
            restauranteId, nombre, hasher.Hashear(request.Contrasena), rol, sucursales, reloj.Ahora);
        usuarios.Agregar(usuario);
        await usuarios.GuardarCambiosAsync(cancellationToken);

        return await resolutor.ADtoAsync(actor, usuario, cancellationToken);
    }
}
