using FluentValidation;
using FluentValidation.Results;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios;

/// <summary>
/// Construye el <see cref="ActorUsuarios"/> de la petición a partir de <see cref="IUsuarioActual"/> (datos vigentes en
/// base de datos) y resuelve las sucursales por slug dentro del restaurante del contexto (filtro global). Scoped.
/// </summary>
public sealed class ResolutorActor(IUsuarioActual usuarioActual, ISucursalRepository sucursales)
{
    public const string MensajeSinAlcance = "No tiene permisos para esta operación.";

    private IReadOnlyList<Sucursal>? _delRestaurante;

    /// <summary>
    /// Actor con su sucursal propia (la única del administrador de sucursal) y si está activa. Lanza
    /// <see cref="ProhibidoException"/> si no puede operar sobre usuarios (004/RF-11, RF-26).
    /// </summary>
    public async Task<ActorUsuarios> ObtenerAsync(CancellationToken ct)
    {
        Guid? propia = null;
        Sucursal? sucursalPropia = null;
        if (usuarioActual.Rol == Rol.AdministradorSucursal && usuarioActual.Sucursales.Count == 1)
        {
            propia = usuarioActual.Sucursales.Single();
            sucursalPropia = await sucursales.ObtenerPorIdAsync(propia.Value, ct);
        }

        var actor = new ActorUsuarios(usuarioActual.Id, usuarioActual.Rol, propia, sucursalPropia?.Activa ?? false);
        if (!AlcanceGestionUsuarios.PuedeOperar(actor))
        {
            throw new ProhibidoException(MensajeSinAlcance);
        }

        return actor;
    }

    /// <summary>
    /// Convierte slugs (sin distinguir mayúsculas) en ids de sucursal del restaurante. Para el administrador de
    /// sucursal, cualquier slug que no sea el de su sucursal es <see cref="ProhibidoException"/>, exista o no
    /// (004/RF-08, RF-10); para el general, un slug inexistente es <see cref="ValidationException"/> en
    /// <c>Sucursales</c> (004/RF-02).
    /// </summary>
    public async Task<IReadOnlyList<Guid>> ResolverSlugsAsync(
        ActorUsuarios actor, IReadOnlyCollection<string> slugs, CancellationToken ct)
    {
        var normalizados = slugs.Select(s => s.Trim().ToLowerInvariant()).ToList();
        var delRestaurante = await DelRestauranteAsync(ct);
        if (actor.Rol == Rol.AdministradorSucursal)
        {
            var slugPropio = delRestaurante.FirstOrDefault(s => s.Id == actor.SucursalPropia)?.Slug;
            if (normalizados.Any(s => s != slugPropio))
            {
                throw new ProhibidoException(MensajeSinAlcance);
            }
        }

        var ids = new List<Guid>(normalizados.Count);
        var fallos = new List<ValidationFailure>();
        foreach (var slug in normalizados)
        {
            var sucursal = delRestaurante.FirstOrDefault(s => s.Slug == slug);
            if (sucursal is null)
            {
                fallos.Add(new ValidationFailure("Sucursales", $"No existe la sucursal {slug}"));
            }
            else
            {
                ids.Add(sucursal.Id);
            }
        }

        if (fallos.Count > 0)
        {
            throw new ValidationException(fallos);
        }

        return ids;
    }

    /// <summary>Proyecta un usuario con sus sucursales ordenadas por nombre y si el actor puede gestionarlo.</summary>
    public async Task<UsuarioDto> ADtoAsync(ActorUsuarios actor, Usuario usuario, CancellationToken ct) =>
        UsuarioDto.Desde(usuario, await DelRestauranteAsync(ct), AlcanceGestionUsuarios.PuedeGestionar(actor, usuario));

    /// <summary>Proyecta varios usuarios conservando su orden (una sola lectura de sucursales).</summary>
    public async Task<IReadOnlyList<UsuarioDto>> ADtosAsync(
        ActorUsuarios actor, IEnumerable<Usuario> usuarios, CancellationToken ct)
    {
        var delRestaurante = await DelRestauranteAsync(ct);
        return usuarios
            .Select(u => UsuarioDto.Desde(u, delRestaurante, AlcanceGestionUsuarios.PuedeGestionar(actor, u)))
            .ToList();
    }

    private async Task<IReadOnlyList<Sucursal>> DelRestauranteAsync(CancellationToken ct) =>
        _delRestaurante ??= await sucursales.ListarAsync(ct);
}
