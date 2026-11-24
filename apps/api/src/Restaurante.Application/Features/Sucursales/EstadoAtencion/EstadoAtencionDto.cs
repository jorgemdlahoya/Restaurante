namespace Restaurante.Application.Features.Sucursales.EstadoAtencion;

/// <summary>Estado de atención de una sucursal: si está abierta y, si no, el mensaje para el cliente.</summary>
public sealed record EstadoAtencionDto(bool Abierta, string? Mensaje);
