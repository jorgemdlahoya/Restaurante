using MediatR;

namespace Restaurante.Application.Features.Sucursales.CambiarEstadoSucursal;

public sealed record CambiarEstadoSucursalCommand(Guid Id, bool? Activa) : IRequest<SucursalDto>;
