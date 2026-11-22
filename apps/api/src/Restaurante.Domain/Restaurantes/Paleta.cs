using System.Text.RegularExpressions;

namespace Restaurante.Domain.Restaurantes;

public sealed partial record Paleta
{
    public static Paleta Vacia { get; } = new(null, null, null, null, null);

    public Paleta(string? principal, string? acento, string? oscuro, string? fondo, string? texto)
    {
        Principal = Validar(principal, nameof(principal));
        Acento = Validar(acento, nameof(acento));
        Oscuro = Validar(oscuro, nameof(oscuro));
        Fondo = Validar(fondo, nameof(fondo));
        Texto = Validar(texto, nameof(texto));
    }

    public string? Principal { get; }
    public string? Acento { get; }
    public string? Oscuro { get; }
    public string? Fondo { get; }
    public string? Texto { get; }

    public static bool EsColorHex(string color) => color is not null && PatronColor().IsMatch(color);

    private static string? Validar(string? color, string parametro) =>
        color is null || EsColorHex(color)
            ? color
            : throw new ArgumentException("El color debe tener el formato #RRGGBB.", parametro);

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex PatronColor();
}
