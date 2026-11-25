namespace Restaurante.Application.Features.Usuarios;

/// <summary>Sucursal asignada a un usuario tal como la expone la API (004/RF-01, RF-03): activa o no.</summary>
public sealed record SucursalAsignadaDto(string Slug, string Nombre, bool Activa);
