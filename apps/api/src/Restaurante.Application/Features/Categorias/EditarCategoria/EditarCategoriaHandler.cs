using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Categorias.EditarCategoria;

public sealed class EditarCategoriaHandler(ICategoriaRepository categorias, ComprobadorUnicidadCategoria unicidad)
    : IRequestHandler<EditarCategoriaCommand, CategoriaDto>
{
    public async Task<CategoriaDto> Handle(EditarCategoriaCommand request, CancellationToken cancellationToken)
    {
        // El repositorio filtra por restaurante y excluye eliminadas: ajena, eliminada e inexistente son lo mismo.
        var categoria = await categorias.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException(MensajesCategoria.NoEncontrada);

        var datos = request.Datos.ADominio();
        // Se excluye su propio id: renombrar "bebidas" a "Bebidas" no choca consigo misma.
        await unicidad.ComprobarAsync(datos.Nombre, datos.NombreCorto, categoria.Id, cancellationToken);

        categoria.Editar(datos);
        await categorias.GuardarCambiosAsync(cancellationToken);
        return CategoriaDto.Desde(categoria);
    }
}
