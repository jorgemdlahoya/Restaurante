namespace Restaurante.Domain.Menu;

/// <summary>Datos editables de una categoría del menú.</summary>
public sealed record DatosCategoria(string Nombre, string? NombreCorto, int Orden);
