using MediatR;

namespace Restaurante.Application.Features.Sucursales.EditarSucursal;

/// <summary>Edita los datos de una sucursal; el slug no cambia nunca.</summary>
public sealed record EditarSucursalCommand(Guid Id, DatosSucursalDto Datos) : IRequest<SucursalDto>;
