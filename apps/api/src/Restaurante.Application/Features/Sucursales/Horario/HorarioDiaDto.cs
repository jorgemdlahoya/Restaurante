using System.Globalization;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales.Horario;

/// <summary>Horario de un día: nombre del día en español, y apertura y cierre <c>HH:mm</c> (sin horas si está cerrado).</summary>
public sealed record HorarioDiaDto(string Dia, bool Cerrado, string? Apertura, string? Cierre)
{
    private const string FormatoHora = "HH\\:mm";

    public static HorarioDiaDto Desde(HorarioDia horario) =>
        new(
            horario.Dia.ComoTexto(),
            horario.Cerrado,
            horario.Apertura?.ToString(FormatoHora, CultureInfo.InvariantCulture),
            horario.Cierre?.ToString(FormatoHora, CultureInfo.InvariantCulture));

    /// <summary>Lee una hora estricta <c>HH:mm</c> de 24 h (dos dígitos en cada parte).</summary>
    public static bool TryLeerHora(string? texto, out TimeOnly hora) =>
        TimeOnly.TryParseExact(texto, FormatoHora, CultureInfo.InvariantCulture, DateTimeStyles.None, out hora);
}
