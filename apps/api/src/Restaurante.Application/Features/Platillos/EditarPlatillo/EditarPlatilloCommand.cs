using MediatR;

namespace Restaurante.Application.Features.Platillos.EditarPlatillo;

public sealed record EditarPlatilloCommand(Guid Id, Guid? CategoriaId, DatosPlatilloDto Datos) : IRequest<PlatilloDto>;
