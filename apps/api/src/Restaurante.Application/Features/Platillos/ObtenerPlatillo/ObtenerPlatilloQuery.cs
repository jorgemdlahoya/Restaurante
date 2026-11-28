using MediatR;

namespace Restaurante.Application.Features.Platillos.ObtenerPlatillo;

public sealed record ObtenerPlatilloQuery(Guid Id) : IRequest<PlatilloDto>;
