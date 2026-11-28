using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Categorias;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Features.Platillos.CrearPlatillo;

public sealed class CrearPlatilloHandler(
    ICategoriaRepository categorias,
    IPlatilloRepository platillos,
    ComprobadorUnicidadPlatillo unicidad,
    IReloj reloj,
    IContextoRestaurante contexto) : IRequestHandler<CrearPlatilloCommand, PlatilloDto>
{
    public async Task<PlatilloDto> Handle(CrearPlatilloCommand request, CancellationToken cancellationToken)
    {
        var restauranteId = contexto.RestauranteId
            ?? throw new InvalidOperationException("No se puede crear un platillo fuera del ámbito de un restaurante.");

        // El repositorio filtra por restaurante y excluye eliminadas: ajena, eliminada e inexistente son lo mismo.
        var categoria = await categorias.ObtenerPorIdAsync(request.CategoriaId, cancellationToken)
            ?? throw new NoEncontradoException(MensajesCategoria.NoEncontrada);

        if (!categoria.Activa)
        {
            throw new ValidationException([new ValidationFailure("categoriaId", "La categoría está inactiva")]);
        }

        var datos = request.Datos.ADominio();
        await unicidad.ComprobarAsync(datos.Nombre, datos.NombreCorto, null, cancellationToken);

        var platillo = Platillo.Crear(restauranteId, categoria.Id, datos, reloj.Ahora);
        platillos.Agregar(platillo);
        await platillos.GuardarCambiosAsync(cancellationToken);

        return PlatilloDto.Desde(platillo, categoria);
    }
}
