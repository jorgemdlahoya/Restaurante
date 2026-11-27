using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Platillos.CambiarEstadoPlatillo;

/// <summary>Activa o desactiva un platillo; sigue en la lista y conserva su nombre reservado (RF-08).</summary>
public sealed class CambiarEstadoPlatilloHandler(ICategoriaRepository categorias, IPlatilloRepository platillos)
    : IRequestHandler<CambiarEstadoPlatilloCommand, PlatilloDto>
{
    public async Task<PlatilloDto> Handle(CambiarEstadoPlatilloCommand request, CancellationToken cancellationToken)
    {
        var platillo = await platillos.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException(MensajesPlatillo.NoEncontrado);
        // Una categoría con platillos no se puede eliminar; si faltara, el platillo se trata como inexistente.
        var categoria = await categorias.ObtenerPorIdAsync(platillo.CategoriaId, cancellationToken)
            ?? throw new NoEncontradoException(MensajesPlatillo.NoEncontrado);

        // El validador (pipeline) garantiza que Activo tiene valor.
        if (request.Activo is true)
        {
            platillo.Activar();
        }
        else
        {
            platillo.Desactivar();
        }

        await platillos.GuardarCambiosAsync(cancellationToken);
        return PlatilloDto.Desde(platillo, categoria);
    }
}
