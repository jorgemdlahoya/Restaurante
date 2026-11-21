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

        if (errores.Count > 0)
        {
            return ResultadoConfiguracion.Invalida(errores);
        }

        var listaOrigenes = origenes!
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        entorno.TryGetValue(VariableAplicarMigraciones, out var migraciones);
        var aplicarMigraciones = string.Equals(migraciones?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

        return ResultadoConfiguracion.Valida(cadenaConexion!, claveJwt!, listaOrigenes, aplicarMigraciones);
    }

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
