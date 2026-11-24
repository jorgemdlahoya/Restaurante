using MediatR;

namespace Restaurante.Application.Features.Sucursales.Horario;

/// <summary>Reemplaza el horario de la semana completa de la sucursal del contexto.</summary>
public sealed record GuardarHorarioCommand(IReadOnlyList<HorarioDiaDto> Dias) : IRequest<IReadOnlyList<HorarioDiaDto>>;
