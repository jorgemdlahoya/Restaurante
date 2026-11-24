using MediatR;

namespace Restaurante.Application.Features.Sucursales.Cierres;

/// <summary>Crea un cierre temporal en la sucursal del contexto. <c>Inicio</c> nulo significa ahora; las fechas son horas locales de la sucursal.</summary>
public sealed record CrearCierreCommand(string? Inicio, string Fin, string? Motivo) : IRequest<CierreTemporalDto>;
