using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales.Cierres;

/// <summary>Cierre temporal con inicio y fin en la hora local de la sucursal (<see cref="HoraLocal.Formato"/>).</summary>
public sealed record CierreTemporalDto(Guid Id, string Inicio, string Fin, string? Motivo)
{
    public static CierreTemporalDto Desde(CierreTemporal cierre, TimeZoneInfo zona) =>
        new(cierre.Id, HoraLocal.DesdeUtc(cierre.Inicio, zona), HoraLocal.DesdeUtc(cierre.Fin, zona), cierre.Motivo);
}

/// <summary>Zona horaria de la sucursal de la ruta, necesaria para leer y mostrar las fechas de los cierres.</summary>
internal static class ZonaDeSucursal
{
    public static async Task<(Sucursal Sucursal, TimeZoneInfo Zona)> ObtenerAsync(
        ISucursalRepository sucursales, IContextoSucursal contexto, CancellationToken ct)
    {
        var sucursal = contexto.SucursalId is { } id ? await sucursales.ObtenerPorIdAsync(id, ct) : null;
        if (sucursal is null)
        {
            throw new NoEncontradoException("Sucursal no disponible");
        }

        return (sucursal, TimeZoneInfo.FindSystemTimeZoneById(sucursal.ZonaHoraria));
    }
}
