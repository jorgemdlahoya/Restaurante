namespace Restaurante.WebApi.Configuracion;

public static class ConfiguracionEntorno
{
    public const string VariableCadenaConexion = "DB_CONNECTION_STRING";
    public const string VariableClaveJwt = "JWT_SIGNING_KEY";
    public const string VariableOrigenesCors = "CORS_ALLOWED_ORIGINS";
    public const string VariableAplicarMigraciones = "APPLY_MIGRATIONS";
    public const int LongitudMinimaClaveJwt = 32;

    public static ResultadoConfiguracion Cargar(IReadOnlyDictionary<string, string?> entorno)
    {
        var errores = new List<string>();

        var cadenaConexion = Obligatoria(entorno, VariableCadenaConexion, errores);
        var claveJwt = Obligatoria(entorno, VariableClaveJwt, errores);
        var origenes = Obligatoria(entorno, VariableOrigenesCors, errores);

        if (claveJwt is not null && claveJwt.Length < LongitudMinimaClaveJwt)
        {
            errores.Add($"{VariableClaveJwt} debe tener al menos {LongitudMinimaClaveJwt} caracteres.");
        }

        var listaOrigenes = origenes is null
            ? []
            : origenes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (origenes is not null)
        {
            ValidarOrigenes(listaOrigenes, errores);
        }

        if (errores.Count > 0)
        {
            return ResultadoConfiguracion.Invalida(errores);
        }

        entorno.TryGetValue(VariableAplicarMigraciones, out var migraciones);
        var aplicarMigraciones = string.Equals(migraciones?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

        return ResultadoConfiguracion.Valida(cadenaConexion!, claveJwt!, listaOrigenes, aplicarMigraciones);
    }

    // Art. 1.4: nunca AllowAnyOrigin. Cada entrada debe ser un origen http(s) absoluto
    // (esquema + host [+ puerto]) sin ruta, query ni barra final. Los errores no incluyen el valor.
    private static void ValidarOrigenes(string[] origenes, List<string> errores)
    {
        if (origenes.Length == 0)
        {
            errores.Add($"{VariableOrigenesCors} no contiene ningún origen.");
            return;
        }

        if (origenes.Contains("*"))
        {
            errores.Add($"{VariableOrigenesCors} no admite el comodín '*'; lista orígenes explícitos.");
        }

        if (origenes.Any(o => o != "*" && !EsOrigenValido(o)))
        {
            errores.Add($"{VariableOrigenesCors} contiene una entrada que no es un origen http/https absoluto " +
                        "sin ruta ni barra final (p. ej. https://app.ejemplo.com).");
        }
    }

    private static bool EsOrigenValido(string valor) =>
        Uri.TryCreate(valor, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && string.Equals(uri.GetLeftPart(UriPartial.Authority), valor, StringComparison.OrdinalIgnoreCase);

    private static string? Obligatoria(
        IReadOnlyDictionary<string, string?> entorno, string nombre, List<string> errores)
    {
        if (entorno.TryGetValue(nombre, out var valor) && !string.IsNullOrWhiteSpace(valor))
        {
            return valor;
        }

        errores.Add($"{nombre} es obligatoria y no está definida.");
        return null;
    }
}
