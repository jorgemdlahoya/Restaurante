using MediatR;
using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Features.Categorias.ListarCategorias;

public sealed class ListarCategoriasHandler(ICategoriaRepository categorias)
    : IRequestHandler<ListarCategoriasQuery, PaginaCategoriasDto>
{
    public async Task<PaginaCategoriasDto> Handle(ListarCategoriasQuery request, CancellationToken cancellationToken)
    {
        var (elementos, total) = await categorias.ListarAsync(request.Pagina, request.Tamano, cancellationToken);
        return new PaginaCategoriasDto(
            elementos.Select(CategoriaDto.Desde).ToList(), total, request.Pagina, request.Tamano);
    }
}
