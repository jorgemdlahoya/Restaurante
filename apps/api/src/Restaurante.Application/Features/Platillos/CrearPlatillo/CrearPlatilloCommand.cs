using MediatR;

namespace Restaurante.Application.Features.Platillos.CrearPlatillo;

public sealed record CrearPlatilloCommand(Guid CategoriaId, DatosPlatilloDto Datos) : IRequest<PlatilloDto>;
