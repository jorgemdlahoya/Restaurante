using MediatR;

namespace Restaurante.Application.Features.Sucursales.ListarSucursales;

public sealed record ListarSucursalesQuery() : IRequest<IReadOnlyList<SucursalDto>>;
