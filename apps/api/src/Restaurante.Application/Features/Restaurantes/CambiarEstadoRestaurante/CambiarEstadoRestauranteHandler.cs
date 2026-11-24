using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Restaurantes.CambiarEstadoRestaurante;

public sealed class CambiarEstadoRestauranteHandler(IRestauranteRepository restaurantes)
    : IRequestHandler<CambiarEstadoRestauranteCommand, RestauranteDto>
{
    public async Task<RestauranteDto> Handle(CambiarEstadoRestauranteCommand request, CancellationToken cancellationToken)
    {
        var restaurante = await restaurantes.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Restaurante no encontrado");

        // El validador (pipeline) garantiza que Activo tiene valor.
        if (request.Activo is true)
        {
            restaurante.Activar();
        }
        else
        {
            restaurante.Desactivar();
        }

        await restaurantes.GuardarCambiosAsync(cancellationToken);
        return RestauranteDto.Desde(restaurante);
    }
}
