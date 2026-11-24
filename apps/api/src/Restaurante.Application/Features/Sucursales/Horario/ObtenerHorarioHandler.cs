using MediatR;
using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Features.Sucursales.Horario;

public sealed class ObtenerHorarioHandler(ISucursalRepository sucursales)
    : IRequestHandler<ObtenerHorarioQuery, IReadOnlyList<HorarioDiaDto>>
{
    public async Task<IReadOnlyList<HorarioDiaDto>> Handle(ObtenerHorarioQuery request, CancellationToken cancellationToken)
    {
        var horario = await sucursales.ObtenerHorarioAsync(cancellationToken);
        return horario.OrderBy(h => h.Dia).Select(HorarioDiaDto.Desde).ToList();
    }
}
