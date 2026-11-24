using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales;

public sealed record SucursalDto(
    Guid Id,
    string Nombre,
    string Direccion,
    string Telefono,
    string Slug,
    string WhatsApp,
    string? PhoneNumberId,
    string Prefijo,
    string ZonaHoraria,
    bool Activa)
{
    public static SucursalDto Desde(Sucursal sucursal) =>
        new(sucursal.Id, sucursal.Nombre, sucursal.Direccion, sucursal.Telefono, sucursal.Slug, sucursal.WhatsApp,
            sucursal.PhoneNumberId, sucursal.Prefijo, sucursal.ZonaHoraria, sucursal.Activa);
}
