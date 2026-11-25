using Restaurante.Domain.Menu;

namespace Restaurante.Application.Features.Categorias;

public sealed record CategoriaDto(Guid Id, string Nombre, string? NombreCorto, int Orden, bool Activa)
{
    public static CategoriaDto Desde(Categoria categoria) =>
        new(categoria.Id, categoria.Nombre, categoria.NombreCorto, categoria.Orden, categoria.Activa);
}
