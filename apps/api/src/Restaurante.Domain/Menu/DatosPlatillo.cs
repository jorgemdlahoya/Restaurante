namespace Restaurante.Domain.Menu;

/// <summary>Datos editables de un platillo del menú (la categoría se pasa aparte).</summary>
public sealed record DatosPlatillo(
    string Nombre,
    string? NombreCorto,
    string? Descripcion,
    int Orden,
    IReadOnlyList<string> Ingredientes);
