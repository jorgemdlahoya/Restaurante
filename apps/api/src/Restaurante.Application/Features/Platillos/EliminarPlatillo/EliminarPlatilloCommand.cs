using MediatR;

namespace Restaurante.Application.Features.Platillos.EliminarPlatillo;

public sealed record EliminarPlatilloCommand(Guid Id) : IRequest;
