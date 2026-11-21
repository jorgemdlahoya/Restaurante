namespace Restaurante.WebApi.Configuracion;

public sealed class ResultadoConfiguracion
{
    private ResultadoConfiguracion(
        IReadOnlyList<string> errores,
        string cadenaConexion,
        string claveJwt,
        IReadOnlyList<string> origenesCors,
        bool aplicarMigraciones)
    {
        Errores = errores;
        CadenaConexion = cadenaConexion;
        ClaveJwt = claveJwt;
        OrigenesCors = origenesCors;
        AplicarMigraciones = aplicarMigraciones;
    }

    public bool EsValida => Errores.Count == 0;

    public IReadOnlyList<string> Errores { get; }

    public string CadenaConexion { get; }

    public string ClaveJwt { get; }

    public IReadOnlyList<string> OrigenesCors { get; }

    public bool AplicarMigraciones { get; }

    public static ResultadoConfiguracion Invalida(IReadOnlyList<string> errores) =>
        new(errores, string.Empty, string.Empty, [], false);

    public static ResultadoConfiguracion Valida(
        string cadenaConexion,
        string claveJwt,
        IReadOnlyList<string> origenesCors,
        bool aplicarMigraciones) =>
        new([], cadenaConexion, claveJwt, origenesCors, aplicarMigraciones);
}
