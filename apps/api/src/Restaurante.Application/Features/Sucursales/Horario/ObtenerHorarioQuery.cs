using MediatR;

namespace Restaurante.Application.Features.Sucursales.Horario;

/// <summary>Horario semanal de la sucursal del contexto, de lunes a domingo.</summary>
public sealed record ObtenerHorarioQuery : IRequest<IReadOnlyList<HorarioDiaDto>>;
