using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Categorias.ObtenerCategoria;

public sealed class ObtenerCategoriaHandler(ICategoriaRepository categorias)
    : IRequestHandler<ObtenerCategoriaQuery, CategoriaDto>
{
    public async Task<CategoriaDto> Handle(ObtenerCategoriaQuery request, CancellationToken cancellationToken)
    {
        var categoria = await categorias.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException(MensajesCategoria.NoEncontrada);
        return CategoriaDto.Desde(categoria);
    }
}
