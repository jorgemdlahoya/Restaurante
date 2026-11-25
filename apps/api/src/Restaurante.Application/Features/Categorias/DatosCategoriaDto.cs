using Restaurante.Domain.Menu;

namespace Restaurante.Application.Features.Categorias;

/// <summary>
/// Datos editables de una categoría tal como llegan por la API. Todo es anulable para que los faltantes
/// los valide <see cref="ValidadorDatosCategoria"/>; el dominio sólo recibe datos ya validados.
/// </summary>
public sealed record DatosCategoriaDto(string? Nombre, string? NombreCorto, int? Orden)
{
    public DatosCategoria ADominio() => new(Nombre ?? string.Empty, NombreCorto, Orden ?? 0);
}
