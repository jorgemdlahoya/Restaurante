using MediatR;
using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Features.Sucursales.Cierres;

public sealed class ListarCierresHandler(
    ISucursalRepository sucursales, ICierreTemporalRepository cierres, IContextoSucursal contexto, IReloj reloj)
    : IRequestHandler<ListarCierresQuery, IReadOnlyList<CierreTemporalDto>>
{
    public async Task<IReadOnlyList<CierreTemporalDto>> Handle(ListarCierresQuery request, CancellationToken cancellationToken)
    {
        var (_, zona) = await ZonaDeSucursal.ObtenerAsync(sucursales, contexto, cancellationToken);
        var vigentes = await cierres.ListarDesdeAsync(reloj.Ahora, cancellationToken);
        return vigentes.Where(c => c.CanceladoEn is null).Select(c => CierreTemporalDto.Desde(c, zona)).ToList();
    }
}
