namespace Restaurante.Domain.Menu;

/// <summary>Única fuente de las reglas del precio de una variante (007/RF-03).</summary>
public static class PrecioVariante
{
    public const decimal Maximo = 99_999.99m;

    /// <summary>Mensajes de error; vacía si el precio es válido. Un nulo sólo produce «obligatorio».</summary>
    public static IReadOnlyList<string> Errores(decimal? precio)
    {
        if (precio is not { } p)
        {
            return ["El precio es obligatorio"];
        }

        var errores = new List<string>();
        if (p < 0)
        {
            errores.Add("El precio no puede ser negativo");
        }
        else if (p > Maximo)
        {
            errores.Add("El precio no puede superar 99,999.99");
        }
        else if (decimal.Round(p, 2) != p)
        {
            errores.Add("El precio debe tener como máximo 2 decimales");
        }

        return errores;
    }
}
