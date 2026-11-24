using System.Globalization;

namespace Restaurante.Application.Features.Sucursales.Cierres;

/// <summary>Fechas de cierres en la API: hora local de la sucursal sin zona (<c>yyyy-MM-ddTHH:mm</c>); se guardan en UTC.</summary>
public static class HoraLocal
{
    public const string Formato = "yyyy-MM-ddTHH:mm";

    private const string FormatoParseo = "yyyy-MM-dd'T'HH:mm";

    /// <summary>Lee una fecha estricta del formato <see cref="Formato"/> (sin zona).</summary>
    public static bool TryLeer(string? texto, out DateTime local) =>
        DateTime.TryParseExact(texto, FormatoParseo, CultureInfo.InvariantCulture, DateTimeStyles.None, out local);

    /// <summary>
    /// Interpreta <paramref name="local"/> en <paramref name="zona"/>. Una hora inexistente (salto de horario de
    /// verano) se corre una hora; una ambigua toma el primer instante.
    /// </summary>
    public static DateTimeOffset AUtc(DateTime local, TimeZoneInfo zona)
    {
        var sinZona = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (zona.IsInvalidTime(sinZona))
        {
            sinZona = sinZona.AddHours(1);
        }

        var desfase = zona.IsAmbiguousTime(sinZona)
            ? zona.GetAmbiguousTimeOffsets(sinZona).Max()
            : zona.GetUtcOffset(sinZona);
        return new DateTimeOffset(sinZona, desfase).ToUniversalTime();
    }

    public static string DesdeUtc(DateTimeOffset utc, TimeZoneInfo zona) =>
        TimeZoneInfo.ConvertTime(utc, zona).ToString(Formato, CultureInfo.InvariantCulture);
}
