namespace Restaurante.Domain.Menu;

/// <summary>Título con el que una categoría se muestra en WhatsApp (límite de 24 unidades UTF-16).</summary>
public static class TituloWhatsApp
{
    public const int Limite = 24;

    /// <remarks>
    /// No recorta espacios: <paramref name="nombre"/> y <paramref name="nombreCorto"/> deben llegar ya normalizados
    /// (sin espacios en los extremos y con el nombre corto en blanco como <c>null</c>), tal como los guarda
    /// <c>Categoria</c>. La versión de TypeScript sí recorta.
    /// </remarks>
    public static string Calcular(string nombre, string? nombreCorto)
    {
        if (!string.IsNullOrWhiteSpace(nombreCorto))
        {
            return nombreCorto;
        }

        if (nombre.Length <= Limite)
        {
            return nombre;
        }

        // Sin partir un par sustituto: si la unidad 23 (índice 22) es sustituto alto, se corta antes.
        var corte = char.IsHighSurrogate(nombre[Limite - 2]) ? Limite - 2 : Limite - 1;
        return nombre[..corte] + "…";
    }

    public static bool SeRecorta(string nombre, string? nombreCorto) =>
        string.IsNullOrWhiteSpace(nombreCorto) && nombre.Length > Limite;
}
