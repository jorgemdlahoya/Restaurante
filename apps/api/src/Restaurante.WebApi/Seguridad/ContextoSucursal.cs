using Restaurante.Application.Abstracciones;

namespace Restaurante.WebApi.Seguridad;

/// <summary>
/// Contexto de sucursal de la petición actual (scoped). Empieza sin sucursal (<c>null</c>) y lo fija
/// <see cref="MiddlewareContextoSucursal"/> cuando la ruta es <c>/api/r/{slug}/s/{sucursal}…</c>.
/// </summary>
public sealed class ContextoSucursal : IContextoSucursal
{
    public Guid? SucursalId { get; private set; }

    public void Establecer(Guid sucursalId) => SucursalId = sucursalId;
}
