using MediatR;

namespace Restaurante.Application.Features.Platillos.CambiarEstadoPlatillo;

public sealed record CambiarEstadoPlatilloCommand(Guid Id, bool? Activo) : IRequest<PlatilloDto>;
