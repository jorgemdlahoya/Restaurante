using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Categorias.EliminarCategoria;

/// <summary>Elimina una categoría (borrado lógico): sale de la lista y libera su nombre y nombre corto (RF-07).</summary>
public sealed class EliminarCategoriaHandler(ICategoriaRepository categorias, IPlatilloRepository platillos, IReloj reloj)
    : IRequestHandler<EliminarCategoriaCommand>
{
    public async Task Handle(EliminarCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await categorias.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException(MensajesCategoria.NoEncontrada);

        // Una categoría con platillos (activos o inactivos) no se elimina; los eliminados no cuentan (006/RF-12).
        var cantidad = await platillos.ContarPorCategoriaAsync(categoria.Id, cancellationToken);
        if (cantidad > 0)
        {
            throw new CategoriaConPlatillosException(cantidad);
        }

        categoria.Eliminar(reloj.Ahora);

        await categorias.GuardarCambiosAsync(cancellationToken);
    }
}
