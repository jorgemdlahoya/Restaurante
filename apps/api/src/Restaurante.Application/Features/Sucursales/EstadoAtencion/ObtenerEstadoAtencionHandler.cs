using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Sucursales.EstadoAtencion;

/// <summary>Calcula el estado de atención con el reloj, el horario y los cierres vigentes; una sucursal inactiva no está disponible.</summary>
public sealed class ObtenerEstadoAtencionHandler(
    ISucursalRepository sucursales, ICierreTemporalRepository cierres, IContextoSucursal contexto, IReloj reloj)
    : IRequestHandler<ObtenerEstadoAtencionQuery, EstadoAtencionDto>
{
    public async Task<EstadoAtencionDto> Handle(ObtenerEstadoAtencionQuery request, CancellationToken cancellationToken)
    {
        var sucursal = contexto.SucursalId is { } id ? await sucursales.ObtenerPorIdAsync(id, cancellationToken) : null;
        if (sucursal is null || !sucursal.Activa)
        {
            throw new SucursalNoDisponibleException();
        }

        var zona = TimeZoneInfo.FindSystemTimeZoneById(sucursal.ZonaHoraria);
        var horario = await sucursales.ObtenerHorarioAsync(cancellationToken);
        var vigentes = await cierres.ListarDesdeAsync(reloj.Ahora, cancellationToken);
        return CalculadoraEstadoAtencion.Calcular(reloj.Ahora, zona, horario, vigentes);
    }
}
