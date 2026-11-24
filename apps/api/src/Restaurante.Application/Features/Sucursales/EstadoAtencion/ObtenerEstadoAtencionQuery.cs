using MediatR;

namespace Restaurante.Application.Features.Sucursales.EstadoAtencion;

/// <summary>Estado de atención público de la sucursal del contexto (RF-12, RF-13, RF-14).</summary>
public sealed record ObtenerEstadoAtencionQuery : IRequest<EstadoAtencionDto>;
