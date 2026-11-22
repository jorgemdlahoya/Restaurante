using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Restaurantes.ObtenerIdentidad;

public sealed class ObtenerIdentidadHandler(IRestauranteRepository restaurantes, IContextoRestaurante contexto)
    : IRequestHandler<ObtenerIdentidadQuery, IdentidadDto>
{
    public async Task<IdentidadDto> Handle(ObtenerIdentidadQuery request, CancellationToken cancellationToken)
    {
        var id = contexto.RestauranteId
            ?? throw new InvalidOperationException("La identidad pública requiere un restaurante en el contexto.");
        var restaurante = await restaurantes.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new NoEncontradoException("Restaurante no disponible");
        var etag = await restaurantes.ObtenerEtagLogoAsync(id, cancellationToken);
        var logoUrl = etag is null ? null : $"/api/r/{restaurante.Slug}/logo?v={etag}";
        return new IdentidadDto(restaurante.Nombre, restaurante.Slug, PaletaDto.Desde(restaurante.Paleta), logoUrl);
    }
}
