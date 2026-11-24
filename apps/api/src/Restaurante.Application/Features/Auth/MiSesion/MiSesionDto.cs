namespace Restaurante.Application.Features.Auth.MiSesion;

/// <summary>Sucursal a la que puede entrar el usuario autenticado.</summary>
public sealed record SucursalDeSesionDto(string Slug, string Nombre);

/// <summary>Datos vigentes de la sesión: usuario, rol y sucursales activas accesibles, ordenadas por nombre.</summary>
public sealed record MiSesionDto(string NombreUsuario, string Rol, IReadOnlyList<SucursalDeSesionDto> Sucursales);
