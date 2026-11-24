using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Restaurantes.ObtenerLogo;

public sealed class ObtenerLogoHandler(IRestauranteRepository restaurantes, IContextoRestaurante contexto)
    : IRequestHandler<ObtenerLogoQuery, LogoDto>
{
    public async Task<LogoDto> Handle(ObtenerLogoQuery request, CancellationToken cancellationToken)
    {
        var id = contexto.RestauranteId
            ?? throw new InvalidOperationException("El logo público requiere un restaurante en el contexto.");
        var logo = await restaurantes.ObtenerLogoAsync(id, cancellationToken)
            ?? throw new NoEncontradoException("El restaurante no tiene logo");
        return new LogoDto(logo.Contenido, logo.Tipo, logo.ETag);
    }
}
