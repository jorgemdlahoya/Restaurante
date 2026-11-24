using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Sucursales.CambiarEstadoSucursal;

/// <summary>Activa o desactiva una sucursal sin tocar su horario ni sus cierres (RF-06).</summary>
public sealed class CambiarEstadoSucursalHandler(ISucursalRepository sucursales)
    : IRequestHandler<CambiarEstadoSucursalCommand, SucursalDto>
{
    public async Task<SucursalDto> Handle(CambiarEstadoSucursalCommand request, CancellationToken cancellationToken)
    {
        var sucursal = await sucursales.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Sucursal no encontrada");

        // El validador (pipeline) garantiza que Activa tiene valor.
        if (request.Activa is true)
        {
            sucursal.Activar();
        }
        else
        {
            sucursal.Desactivar();
        }

        await sucursales.GuardarCambiosAsync(cancellationToken);
        return SucursalDto.Desde(sucursal);
    }
}
