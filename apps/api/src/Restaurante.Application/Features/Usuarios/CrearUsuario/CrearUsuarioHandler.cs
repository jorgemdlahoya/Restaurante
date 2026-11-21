using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios.CrearUsuario;

public sealed class CrearUsuarioHandler(
    IUsuarioRepository usuarios,
    IHasherContrasenas hasher,
    IReloj reloj) : IRequestHandler<CrearUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> Handle(CrearUsuarioCommand request, CancellationToken cancellationToken)
    {
        var nombre = request.NombreUsuario.Trim();
        var existente = await usuarios.ObtenerPorNombreNormalizadoAsync(nombre.ToLowerInvariant(), cancellationToken);
        if (existente is not null)
        {
            throw new ConflictoException("Ya existe un usuario con ese nombre");
        }

        var rol = Enum.Parse<Rol>(request.Rol, ignoreCase: true);
        var usuario = Usuario.Crear(nombre, hasher.Hashear(request.Contrasena), rol, reloj.Ahora);
        usuarios.Agregar(usuario);
        await usuarios.GuardarCambiosAsync(cancellationToken);

        return UsuarioDto.Desde(usuario);
    }
}
