using MediatR;

namespace Restaurante.Application.Features.Sucursales.Cierres;

/// <summary>Cancela un cierre vigente o futuro de la sucursal del contexto.</summary>
public sealed record CancelarCierreCommand(Guid Id) : IRequest<CierreTemporalDto>;
