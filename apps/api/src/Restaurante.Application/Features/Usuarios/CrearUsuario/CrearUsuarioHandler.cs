using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios.CrearUsuario;

public sealed class CrearUsuarioHandler(
    IUsuarioRepository usuarios,
    IHasherContrasenas hasher,
    IReloj reloj,
    IContextoRestaurante contexto) : IRequestHandler<CrearUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> Handle(CrearUsuarioCommand request, CancellationToken cancellationToken)
    {
        var restauranteId = contexto.RestauranteId
            ?? throw new InvalidOperationException("No se puede crear un usuario fuera del ámbito de un restaurante.");

        var nombre = request.NombreUsuario.Trim();
        var existente = await usuarios.ObtenerPorNombreNormalizadoAsync(nombre.ToLowerInvariant(), cancellationToken);
        if (existente is not null)
        {
            throw new ConflictoException("Ya existe un usuario con ese nombre");
        }

        var rol = RolExtensiones.DesdeTexto(request.Rol);
        var usuario = Usuario.Crear(restauranteId, nombre, hasher.Hashear(request.Contrasena), rol, [], reloj.Ahora);
        usuarios.Agregar(usuario);
        await usuarios.GuardarCambiosAsync(cancellationToken);

        return UsuarioDto.Desde(usuario);
    }
}
