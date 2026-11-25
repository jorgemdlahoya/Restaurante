using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Features.Categorias.CrearCategoria;

public sealed class CrearCategoriaHandler(
    ICategoriaRepository categorias,
    ComprobadorUnicidadCategoria unicidad,
    IReloj reloj,
    IContextoRestaurante contexto) : IRequestHandler<CrearCategoriaCommand, CategoriaDto>
{
    public async Task<CategoriaDto> Handle(CrearCategoriaCommand request, CancellationToken cancellationToken)
    {
        var restauranteId = contexto.RestauranteId
            ?? throw new InvalidOperationException("No se puede crear una categoría fuera del ámbito de un restaurante.");

        var datos = request.Datos.ADominio();
        await unicidad.ComprobarAsync(datos.Nombre, datos.NombreCorto, null, cancellationToken);

        var categoria = Categoria.Crear(restauranteId, datos, reloj.Ahora);
        categorias.Agregar(categoria);
        await categorias.GuardarCambiosAsync(cancellationToken);

        return CategoriaDto.Desde(categoria);
    }
}
