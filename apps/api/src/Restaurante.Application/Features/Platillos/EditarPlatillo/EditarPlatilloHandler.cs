using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Platillos.EditarPlatillo;

/// <summary>Edita los datos de un platillo y, si cambia la categoría, lo mueve (RF-07).</summary>
public sealed class EditarPlatilloHandler(
    ICategoriaRepository categorias,
    IPlatilloRepository platillos,
    ComprobadorUnicidadPlatillo unicidad) : IRequestHandler<EditarPlatilloCommand, PlatilloDto>
{
    public async Task<PlatilloDto> Handle(EditarPlatilloCommand request, CancellationToken cancellationToken)
    {
        // El repositorio filtra por restaurante y excluye eliminados: ajeno, eliminado e inexistente son lo mismo.
        var platillo = await platillos.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException(MensajesPlatillo.NoEncontrado);

        // El validador (pipeline) garantiza que CategoriaId tiene valor.
        var categoriaId = request.CategoriaId ?? platillo.CategoriaId;
        var categoria = await categorias.ObtenerPorIdAsync(categoriaId, cancellationToken)
            ?? throw new ValidationException([new ValidationFailure("categoriaId", "La categoría no existe")]);

        // Quedarse en la categoría actual está permitido aunque esté inactiva; moverse a una inactiva, no.
        if (!categoria.Activa && categoria.Id != platillo.CategoriaId)
        {
            throw new ValidationException([new ValidationFailure("categoriaId", "La categoría está inactiva")]);
        }

        var datos = request.Datos.ADominio();
        // Se excluye su propio id: renombrar "pozole" a "Pozole" no choca consigo mismo.
        await unicidad.ComprobarAsync(datos.Nombre, datos.NombreCorto, platillo.Id, cancellationToken);

        platillo.Editar(categoria.Id, datos);
        await platillos.GuardarCambiosAsync(cancellationToken);
        return PlatilloDto.Desde(platillo, categoria);
    }
}
