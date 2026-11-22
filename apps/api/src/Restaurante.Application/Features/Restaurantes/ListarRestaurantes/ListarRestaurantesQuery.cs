using MediatR;

namespace Restaurante.Application.Features.Restaurantes.ListarRestaurantes;

public sealed record ListarRestaurantesQuery() : IRequest<IReadOnlyList<RestauranteDto>>;
