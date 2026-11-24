using System.Text.RegularExpressions;

namespace Restaurante.Domain.Comun;

public static partial class TelefonoMexico
{
    /// <summary>
    /// Quita espacios y guiones y devuelve el número en E.164 (+52XXXXXXXXXX),
    /// o <c>null</c> si no cumple el formato.
    /// </summary>
    public static string? Normalizar(string? texto)
    {
        if (texto is null)
        {
            return null;
        }

        var limpio = texto.Replace(" ", string.Empty).Replace("-", string.Empty);
        return PatronE164().IsMatch(limpio) ? limpio : null;
    }

    [GeneratedRegex(@"^\+52[0-9]{10}\z")]
    private static partial Regex PatronE164();
}
