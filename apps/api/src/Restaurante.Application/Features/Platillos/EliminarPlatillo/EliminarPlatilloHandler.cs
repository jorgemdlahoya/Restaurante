using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Platillos.EliminarPlatillo;

/// <summary>Elimina un platillo (borrado lógico): sale de la lista y libera su nombre y nombre corto (RF-09).</summary>
public sealed class EliminarPlatilloHandler(IPlatilloRepository platillos, IReloj reloj)
    : IRequestHandler<EliminarPlatilloCommand>
{
    public async Task Handle(EliminarPlatilloCommand request, CancellationToken cancellationToken)
    {
        var platillo = await platillos.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException(MensajesPlatillo.NoEncontrado);

        platillo.Eliminar(reloj.Ahora);

        await platillos.GuardarCambiosAsync(cancellationToken);
    }
}
