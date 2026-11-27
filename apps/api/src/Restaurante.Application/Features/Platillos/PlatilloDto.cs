using Restaurante.Domain.Menu;

namespace Restaurante.Application.Features.Platillos;

public sealed record PlatilloDto(
    Guid Id,
    Guid CategoriaId,
    string CategoriaNombre,
    string Nombre,
    string? NombreCorto,
    string? Descripcion,
    int Orden,
    IReadOnlyList<string> Ingredientes,
    bool Activo)
{
    public static PlatilloDto Desde(Platillo platillo, Categoria categoria) =>
        new(platillo.Id, platillo.CategoriaId, categoria.Nombre, platillo.Nombre, platillo.NombreCorto,
            platillo.Descripcion, platillo.Orden, platillo.Ingredientes, platillo.Activo);
}
