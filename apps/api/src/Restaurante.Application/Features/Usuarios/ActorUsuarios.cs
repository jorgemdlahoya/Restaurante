using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios;

/// <summary>
/// Usuario que ejecuta la operación de gestión, con los datos vigentes en base de datos.
/// <paramref name="SucursalPropia"/> es la única sucursal del administrador de sucursal
/// (<c>null</c> para los demás roles).
/// </summary>
public sealed record ActorUsuarios(Guid Id, Rol Rol, Guid? SucursalPropia, bool SucursalPropiaActiva);
