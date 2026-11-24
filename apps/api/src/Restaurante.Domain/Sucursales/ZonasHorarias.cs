namespace Restaurante.Domain.Sucursales;

public static class ZonasHorarias
{
    public const string PorDefecto = "America/Mexico_City";

    public static IReadOnlyList<string> Admitidas { get; } =
    [
        "America/Mexico_City",
        "America/Cancun",
        "America/Merida",
        "America/Monterrey",
        "America/Matamoros",
        "America/Chihuahua",
        "America/Ciudad_Juarez",
        "America/Ojinaga",
        "America/Mazatlan",
        "America/Bahia_Banderas",
        "America/Hermosillo",
        "America/Tijuana",
    ];

    public static bool EsAdmitida(string? zona) =>
        zona is not null && Admitidas.Contains(zona, StringComparer.Ordinal);
}
