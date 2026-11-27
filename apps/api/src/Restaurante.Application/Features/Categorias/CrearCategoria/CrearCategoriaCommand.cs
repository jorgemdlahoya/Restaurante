using MediatR;

namespace Restaurante.Application.Features.Categorias.CrearCategoria;

public sealed record CrearCategoriaCommand(DatosCategoriaDto Datos) : IRequest<CategoriaDto>;
