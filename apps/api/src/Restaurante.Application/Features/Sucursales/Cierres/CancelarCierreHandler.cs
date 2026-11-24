using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Sucursales.Cierres;

/// <summary>
/// Cancela un cierre (RF-11). Uno inexistente, de otra sucursal, ya cancelado o ya terminado responde 404:
/// <c>CierreTemporal.Cancelar</c> no es idempotente y reactivaría el tramo de un cierre terminado.
/// </summary>
public sealed class CancelarCierreHandler(
    ISucursalRepository sucursales, ICierreTemporalRepository cierres, IContextoSucursal contexto, IReloj reloj)
    : IRequestHandler<CancelarCierreCommand, CierreTemporalDto>
{
    public async Task<CierreTemporalDto> Handle(CancelarCierreCommand request, CancellationToken cancellationToken)
    {
        var (_, zona) = await ZonaDeSucursal.ObtenerAsync(sucursales, contexto, cancellationToken);
        var ahora = reloj.Ahora;

        var cierre = await cierres.ObtenerPorIdAsync(request.Id, cancellationToken);
        if (cierre is null || cierre.CanceladoEn is not null || cierre.Fin <= ahora)
        {
            throw new NoEncontradoException("Cierre no encontrado");
        }

        cierre.Cancelar(ahora);
        await cierres.GuardarCambiosAsync(cancellationToken);
        return CierreTemporalDto.Desde(cierre, zona);
    }
}
