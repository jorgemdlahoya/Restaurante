using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales.Horario;

/// <summary>Reemplaza los siete días del horario (RF-08); un cierre anterior a la apertura es un turno nocturno (RF-09).</summary>
public sealed class GuardarHorarioHandler(ISucursalRepository sucursales)
    : IRequestHandler<GuardarHorarioCommand, IReadOnlyList<HorarioDiaDto>>
{
    public async Task<IReadOnlyList<HorarioDiaDto>> Handle(GuardarHorarioCommand request, CancellationToken cancellationToken)
    {
        var filas = await sucursales.ObtenerHorarioAsync(cancellationToken);

        // El validador (pipeline) garantiza siete días distintos y horas válidas.
        foreach (var dto in request.Dias)
        {
            DiasSemana.TryDesdeTexto(dto.Dia, out var dia);
            var fila = filas.SingleOrDefault(f => f.Dia == dia)
                ?? throw new InvalidOperationException($"La sucursal no tiene la fila de horario del día {dto.Dia}.");

            if (dto.Cerrado)
            {
                fila.Cambiar(null, null);
            }
            else
            {
                HorarioDiaDto.TryLeerHora(dto.Apertura, out var apertura);
                HorarioDiaDto.TryLeerHora(dto.Cierre, out var cierre);
                fila.Cambiar(apertura, cierre);
            }
        }

        await sucursales.GuardarCambiosAsync(cancellationToken);
        return filas.OrderBy(f => f.Dia).Select(HorarioDiaDto.Desde).ToList();
    }
}
