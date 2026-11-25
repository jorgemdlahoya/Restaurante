using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Auth.MiSesion;

public sealed class ObtenerMiSesionHandler(
    IUsuarioActual actual,
    IUsuarioRepository usuarios,
    ISucursalRepository sucursales) : IRequestHandler<ObtenerMiSesionQuery, MiSesionDto>
{
    public async Task<MiSesionDto> Handle(ObtenerMiSesionQuery request, CancellationToken cancellationToken)
    {
        // El nombre sale de la base de datos, no del token.
        var usuario = await usuarios.ObtenerPorIdAsync(actual.Id, cancellationToken)
            ?? throw new NoEncontradoException("Usuario no encontrado");

        // Se conserva el orden del repositorio (por nombre, con la intercalación de la base de datos),
        // el mismo que usa UsuarioDto: reordenar aquí con otra comparación daría órdenes distintos.
        var todas = await sucursales.ListarAsync(cancellationToken);
        var accesibles = todas
            .Where(s => s.Activa && (actual.Rol == Rol.AdministradorGeneral || actual.Sucursales.Contains(s.Id)))
            .Select(s => new SucursalDeSesionDto(s.Slug, s.Nombre))
            .ToList();

        return new MiSesionDto(usuario.NombreUsuario, actual.Rol.ComoTexto(), accesibles);
    }
}
