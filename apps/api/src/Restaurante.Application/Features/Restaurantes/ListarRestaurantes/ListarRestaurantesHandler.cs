using MediatR;
using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Features.Restaurantes.ListarRestaurantes;

public sealed class ListarRestaurantesHandler(IRestauranteRepository restaurantes)
    : IRequestHandler<ListarRestaurantesQuery, IReadOnlyList<RestauranteDto>>
{
    public async Task<IReadOnlyList<RestauranteDto>> Handle(ListarRestaurantesQuery request, CancellationToken cancellationToken)
    {
        var lista = await restaurantes.ListarAsync(cancellationToken);
        return lista.Select(RestauranteDto.Desde).ToList();
    }
}
