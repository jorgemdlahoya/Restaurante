using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Restaurantes;

namespace Restaurante.Application.Features.Restaurantes.EditarRestaurante;

public sealed class EditarRestauranteHandler(IRestauranteRepository restaurantes)
    : IRequestHandler<EditarRestauranteCommand, RestauranteDto>
{
    public async Task<RestauranteDto> Handle(EditarRestauranteCommand request, CancellationToken cancellationToken)
    {
        var restaurante = await restaurantes.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Restaurante no encontrado");

        var nombre = request.Nombre.Trim();
        if (await restaurantes.ExisteNombreAsync(nombre, request.Id, cancellationToken))
        {
            throw new ConflictoException("Ya existe un restaurante con ese nombre");
        }

        restaurante.CambiarNombre(nombre);
        restaurante.CambiarPaleta(request.Paleta?.ADominio() ?? Paleta.Vacia);
        await restaurantes.GuardarCambiosAsync(cancellationToken);
        return RestauranteDto.Desde(restaurante);
    }
}
