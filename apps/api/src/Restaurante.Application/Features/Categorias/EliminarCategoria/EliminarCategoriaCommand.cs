using MediatR;

namespace Restaurante.Application.Features.Categorias.EliminarCategoria;

public sealed record EliminarCategoriaCommand(Guid Id) : IRequest;
