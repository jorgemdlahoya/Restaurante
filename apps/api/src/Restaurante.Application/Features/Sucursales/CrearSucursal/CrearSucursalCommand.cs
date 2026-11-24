using MediatR;

namespace Restaurante.Application.Features.Sucursales.CrearSucursal;

public sealed record CrearSucursalCommand(string Slug, DatosSucursalDto Datos) : IRequest<SucursalDto>;
