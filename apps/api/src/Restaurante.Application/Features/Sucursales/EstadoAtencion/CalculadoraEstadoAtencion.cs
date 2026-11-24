using System.Globalization;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales.EstadoAtencion;

/// <summary>
/// Decide si una sucursal está abierta en un instante y, si no, cuándo vuelve a abrir (RF-12, RF-13).
/// Función pura: no consulta el reloj ni hace E/S; el instante y la zona llegan como parámetros.
/// </summary>
public static class CalculadoraEstadoAtencion
{
    public const int DiasDeBusqueda = 14;

    public const string MensajeSinApertura = "Estamos cerrados por el momento";

    public static EstadoAtencionDto Calcular(
        DateTimeOffset ahora, TimeZoneInfo zona, IReadOnlyList<HorarioDia> horario, IReadOnlyList<CierreTemporal> cierres)
    {
        var intervalos = GenerarIntervalos(ahora, zona, horario)
            .SelectMany(i => RestarCierres(i, cierres))
            .ToList();

        if (intervalos.Any(i => i.Inicio <= ahora && ahora < i.Fin))
        {
            return new EstadoAtencionDto(true, null);
        }

        var limite = ahora.AddDays(DiasDeBusqueda);
        var proximas = intervalos.Where(i => i.Inicio > ahora && i.Inicio <= limite).ToList();
        if (proximas.Count == 0)
        {
            return new EstadoAtencionDto(false, MensajeSinApertura);
        }

        var apertura = TimeZoneInfo.ConvertTime(proximas.Min(i => i.Inicio), zona);
        var hoy = TimeZoneInfo.ConvertTime(ahora, zona).Date;
        var dias = (apertura.Date - hoy).Days;
        var dia = dias switch
        {
            0 => "hoy",
            1 => "mañana",
            _ => DiasSemana.Desde(apertura.DayOfWeek).ComoTexto(),
        };
        var hora = apertura.ToString("HH:mm", CultureInfo.InvariantCulture);

        return new EstadoAtencionDto(false, $"Estamos cerrados, abrimos {dia} a las {hora}");
    }

    private static IEnumerable<Intervalo> GenerarIntervalos(
        DateTimeOffset ahora, TimeZoneInfo zona, IReadOnlyList<HorarioDia> horario)
    {
        var porDia = horario.Where(h => !h.Cerrado).ToDictionary(h => h.Dia);
        var hoy = TimeZoneInfo.ConvertTime(ahora, zona).Date;

        for (var desplazamiento = -1; desplazamiento <= DiasDeBusqueda; desplazamiento++)
        {
            var fecha = DateTime.SpecifyKind(hoy.AddDays(desplazamiento), DateTimeKind.Unspecified);
            if (!porDia.TryGetValue(DiasSemana.Desde(fecha.DayOfWeek), out var dia))
            {
                continue;
            }

            var inicioLocal = fecha + dia.Apertura!.Value.ToTimeSpan();
            var finLocal = fecha.AddDays(dia.CruzaMedianoche ? 1 : 0) + dia.Cierre!.Value.ToTimeSpan();

            yield return new Intervalo(AUtc(inicioLocal, zona), AUtc(finLocal, zona));
        }
    }

    private static DateTimeOffset AUtc(DateTime local, TimeZoneInfo zona)
    {
        // Una hora que no existe por el cambio de horario se recorre una hora hacia adelante.
        if (zona.IsInvalidTime(local))
        {
            local = local.AddHours(1);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zona), TimeSpan.Zero);
    }

    private static IEnumerable<Intervalo> RestarCierres(Intervalo intervalo, IReadOnlyList<CierreTemporal> cierres)
    {
        IEnumerable<Intervalo> restantes = [intervalo];
        foreach (var cierre in cierres)
        {
            restantes = restantes.SelectMany(i => Restar(i, cierre.Inicio, cierre.Fin)).ToList();
        }

        return restantes;
    }

    private static IEnumerable<Intervalo> Restar(Intervalo i, DateTimeOffset inicioCierre, DateTimeOffset finCierre)
    {
        if (finCierre <= i.Inicio || inicioCierre >= i.Fin)
        {
            yield return i;
            yield break;
        }

        if (inicioCierre > i.Inicio)
        {
            yield return new Intervalo(i.Inicio, inicioCierre);
        }

        if (finCierre < i.Fin)
        {
            yield return new Intervalo(finCierre, i.Fin);
        }
    }

    private readonly record struct Intervalo(DateTimeOffset Inicio, DateTimeOffset Fin);
}
