namespace Restaurante.Domain.Menu;

/// <summary>Regla de qué variantes de un platillo se muestran a los clientes (007/RF-14).</summary>
public static class VariantesALaVenta
{
    /// <summary>
    /// Variantes activas y no eliminadas del platillo, si la categoría y el platillo están activos y no eliminados,
    /// ordenadas por precio, nombre (ordinal) e id.
    /// </summary>
    public static IReadOnlyList<Variante> De(Categoria categoria, Platillo platillo, IEnumerable<Variante> variantes)
    {
        ArgumentNullException.ThrowIfNull(categoria);
        ArgumentNullException.ThrowIfNull(platillo);
        ArgumentNullException.ThrowIfNull(variantes);

        if (!categoria.Activa || categoria.EliminadaEn is not null
            || !platillo.Activo || platillo.EliminadaEn is not null)
        {
            return [];
        }

        return variantes
            .Where(v => v.PlatilloId == platillo.Id && v.Activa && v.EliminadaEn is null)
            .OrderBy(v => v.Precio)
            .ThenBy(v => v.Nombre, StringComparer.Ordinal)
            .ThenBy(v => v.Id)
            .ToList();
    }
}
