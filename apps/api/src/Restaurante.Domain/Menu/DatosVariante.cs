namespace Restaurante.Domain.Menu;

/// <summary>Datos editables de una variante de platillo (el platillo se pasa aparte).</summary>
public sealed record DatosVariante(string Nombre, decimal Precio, string? Sku);
