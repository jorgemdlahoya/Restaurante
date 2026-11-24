using MediatR;
using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Features.Sucursales.ListarSucursales;

public sealed class ListarSucursalesHandler(ISucursalRepository sucursales)
    : IRequestHandler<ListarSucursalesQuery, IReadOnlyList<SucursalDto>>
{
    public async Task<IReadOnlyList<SucursalDto>> Handle(ListarSucursalesQuery request, CancellationToken cancellationToken)
    {
        var lista = await sucursales.ListarAsync(cancellationToken);
        return lista.Select(SucursalDto.Desde).ToList();
    }
}
