using MediatR;

namespace Restaurante.Application.Features.Categorias.EditarCategoria;

public sealed record EditarCategoriaCommand(Guid Id, DatosCategoriaDto Datos) : IRequest<CategoriaDto>;
