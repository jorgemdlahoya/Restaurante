using MediatR;

namespace Restaurante.Application.Features.Sucursales.Cierres;

/// <summary>Cierres vigentes y futuros (<c>Fin</c> posterior a ahora) de la sucursal del contexto.</summary>
public sealed record ListarCierresQuery : IRequest<IReadOnlyList<CierreTemporalDto>>;
