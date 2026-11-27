namespace Restaurante.Application.Features.Categorias;

public sealed record PaginaCategoriasDto(IReadOnlyList<CategoriaDto> Elementos, int Total, int Pagina, int Tamano);
