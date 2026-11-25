using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Categorias.CambiarEstadoCategoria;

/// <summary>Activa o desactiva una categoría; sigue en la lista y conserva su nombre reservado (RF-06).</summary>
public sealed class CambiarEstadoCategoriaHandler(ICategoriaRepository categorias)
    : IRequestHandler<CambiarEstadoCategoriaCommand, CategoriaDto>
{
    public async Task<CategoriaDto> Handle(CambiarEstadoCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await categorias.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException(MensajesCategoria.NoEncontrada);

        // El validador (pipeline) garantiza que Activa tiene valor.
        if (request.Activa is true)
        {
            categoria.Activar();
        }
        else
        {
            categoria.Desactivar();
        }

        await categorias.GuardarCambiosAsync(cancellationToken);
        return CategoriaDto.Desde(categoria);
    }
}
