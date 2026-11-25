using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Categorias.EliminarCategoria;

/// <summary>Elimina una categoría (borrado lógico): sale de la lista y libera su nombre y nombre corto (RF-07).</summary>
public sealed class EliminarCategoriaHandler(ICategoriaRepository categorias, IReloj reloj)
    : IRequestHandler<EliminarCategoriaCommand>
{
    public async Task Handle(EliminarCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await categorias.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException(MensajesCategoria.NoEncontrada);

        categoria.Eliminar(reloj.Ahora);

        await categorias.GuardarCambiosAsync(cancellationToken);
    }
}
