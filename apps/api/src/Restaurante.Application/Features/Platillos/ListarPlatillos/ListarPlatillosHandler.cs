using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Categorias;

namespace Restaurante.Application.Features.Platillos.ListarPlatillos;

public sealed class ListarPlatillosHandler(ICategoriaRepository categorias, IPlatilloRepository platillos)
    : IRequestHandler<ListarPlatillosQuery, PaginaPlatillosDto>
{
    public async Task<PaginaPlatillosDto> Handle(ListarPlatillosQuery request, CancellationToken cancellationToken)
    {
        // Ajena, eliminada e inexistente son lo mismo; una categoría inactiva sí lista sus platillos.
        var categoria = await categorias.ObtenerPorIdAsync(request.CategoriaId, cancellationToken)
            ?? throw new NoEncontradoException(MensajesCategoria.NoEncontrada);

        var (elementos, total) = await platillos.ListarPorCategoriaAsync(
            categoria.Id, request.Pagina, request.Tamano, cancellationToken);
        return new PaginaPlatillosDto(
            elementos.Select(p => PlatilloDto.Desde(p, categoria)).ToList(), total, request.Pagina, request.Tamano);
    }
}
