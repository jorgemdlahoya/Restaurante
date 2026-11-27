using MediatR;

namespace Restaurante.Application.Features.Categorias.ObtenerCategoria;

public sealed record ObtenerCategoriaQuery(Guid Id) : IRequest<CategoriaDto>;
