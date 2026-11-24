using Restaurante.Application.Comun;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios;

/// <summary>
/// Usuario tal como lo expone la API, sin contraseña ni hash. <see cref="Sucursales"/> van ordenadas por nombre;
/// <see cref="Gestionable"/> indica si quien consulta puede modificarlo (004/RF-07).
/// </summary>
public sealed record UsuarioDto(
    Guid Id,
    string NombreUsuario,
    string Rol,
    bool Activo,
    IReadOnlyList<SucursalAsignadaDto> Sucursales,
    bool Gestionable)
{
    /// <param name="sucursalesDelRestaurante">Sucursales del restaurante ordenadas por nombre (las de <c>ListarAsync</c>).</param>
    public static UsuarioDto Desde(Usuario usuario, IReadOnlyList<Sucursal> sucursalesDelRestaurante, bool gestionable)
    {
        var asignadas = usuario.Sucursales.ToHashSet();
        return new(
            usuario.Id,
            usuario.NombreUsuario,
            usuario.Rol.ComoTexto(),
            usuario.Activo,
            sucursalesDelRestaurante
                .Where(s => asignadas.Contains(s.Id))
                .Select(s => new SucursalAsignadaDto(s.Slug, s.Nombre, s.Activa))
                .ToList(),
            gestionable);
    }
}
