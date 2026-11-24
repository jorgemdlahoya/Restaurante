using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales;

/// <summary>Datos editables de una sucursal tal como llegan por la API; una zona nula usa la de por defecto.</summary>
public sealed record DatosSucursalDto(
    string Nombre,
    string Direccion,
    string Telefono,
    string WhatsApp,
    string? PhoneNumberId,
    string Prefijo,
    string? ZonaHoraria)
{
    public DatosSucursal ADominio() =>
        new(Nombre, Direccion, Telefono, WhatsApp, PhoneNumberId, Prefijo, ZonaHoraria ?? ZonasHorarias.PorDefecto);
}
