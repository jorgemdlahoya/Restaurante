using MediatR;

namespace Restaurante.Application.Features.Categorias.ListarCategorias;

public sealed record ListarCategoriasQuery(int Pagina, int Tamano) : IRequest<PaginaCategoriasDto>;
