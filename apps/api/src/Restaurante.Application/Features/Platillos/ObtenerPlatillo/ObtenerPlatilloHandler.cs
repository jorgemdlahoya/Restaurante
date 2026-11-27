using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Platillos.ObtenerPlatillo;

public sealed class ObtenerPlatilloHandler(ICategoriaRepository categorias, IPlatilloRepository platillos)
    : IRequestHandler<ObtenerPlatilloQuery, PlatilloDto>
{
    public async Task<PlatilloDto> Handle(ObtenerPlatilloQuery request, CancellationToken cancellationToken)
    {
        var platillo = await platillos.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException(MensajesPlatillo.NoEncontrado);

        // Una categoría con platillos no se puede eliminar; si faltara, el platillo se trata como inexistente.
        var categoria = await categorias.ObtenerPorIdAsync(platillo.CategoriaId, cancellationToken)
            ?? throw new NoEncontradoException(MensajesPlatillo.NoEncontrado);

        return PlatilloDto.Desde(platillo, categoria);
    }
}
