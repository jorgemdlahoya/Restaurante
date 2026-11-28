using Restaurante.Domain.Menu;

namespace Restaurante.Application.Features.Platillos;

/// <summary>
/// Datos editables de un platillo tal como llegan por la API (la categoría va aparte). Todo es anulable para que
/// los faltantes los valide <see cref="ValidadorDatosPlatillo"/>; el dominio sólo recibe datos ya validados.
/// </summary>
public sealed record DatosPlatilloDto(
    string? Nombre,
    string? NombreCorto,
    string? Descripcion,
    int? Orden,
    IReadOnlyList<string?>? Ingredientes)
{
    public DatosPlatillo ADominio() => new(
        Nombre ?? string.Empty,
        NombreCorto,
        Descripcion,
        Orden ?? 0,
        IngredientesPlatillo.Normalizar(Ingredientes));
}
