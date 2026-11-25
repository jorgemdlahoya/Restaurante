using MediatR;

namespace Restaurante.Application.Features.Categorias.CambiarEstadoCategoria;

public sealed record CambiarEstadoCategoriaCommand(Guid Id, bool? Activa) : IRequest<CategoriaDto>;
