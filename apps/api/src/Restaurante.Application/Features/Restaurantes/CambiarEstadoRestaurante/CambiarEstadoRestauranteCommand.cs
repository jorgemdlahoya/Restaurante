using MediatR;

namespace Restaurante.Application.Features.Restaurantes.CambiarEstadoRestaurante;

public sealed record CambiarEstadoRestauranteCommand(Guid Id, bool? Activo) : IRequest<RestauranteDto>;
