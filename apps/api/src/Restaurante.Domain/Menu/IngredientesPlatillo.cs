namespace Restaurante.Domain.Menu;

/// <summary>Única fuente de las reglas de la lista de ingredientes de un platillo (006/RF-02).</summary>
public static class IngredientesPlatillo
{
    public const int MaximoElementos = 30;
    public const int LongitudMaxima = 40;

    /// <summary>Mensajes de error en orden de lista; vacía si todo está bien. Un nulo cuenta como vacío.</summary>
    public static IReadOnlyList<string> Errores(IReadOnlyList<string?>? ingredientes)
    {
        var errores = new List<string>();
        if (ingredientes is null)
        {
            return errores;
        }

        if (ingredientes.Count > MaximoElementos)
        {
            errores.Add($"No puede haber más de {MaximoElementos} ingredientes");
        }

        var vistos = new HashSet<string>();
        for (var i = 0; i < ingredientes.Count; i++)
        {
            var n = i + 1;
            var texto = ingredientes[i]?.Trim() ?? string.Empty;
            if (texto.Length is 0)
            {
                errores.Add($"El ingrediente {n} está vacío");
            }
            else if (texto.Length > LongitudMaxima)
            {
                errores.Add($"El ingrediente {n} no puede superar {LongitudMaxima} caracteres");
            }
            else if (!vistos.Add(Categoria.Normalizar(texto)))
            {
                errores.Add($"El ingrediente {n} está repetido");
            }
        }

        return errores;
    }

    /// <summary>Recorta cada ingrediente conservando el orden; lanza si <see cref="Errores"/> no está vacía.</summary>
    public static IReadOnlyList<string> Normalizar(IReadOnlyList<string?>? ingredientes)
    {
        var errores = Errores(ingredientes);
        if (errores.Count > 0)
        {
            throw new ArgumentException(string.Join("; ", errores), nameof(ingredientes));
        }

        return ingredientes is null ? [] : ingredientes.Select(i => i!.Trim()).ToList();
    }
}
