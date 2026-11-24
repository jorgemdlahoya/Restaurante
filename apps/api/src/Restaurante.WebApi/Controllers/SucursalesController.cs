using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Sucursales;
using Restaurante.Application.Features.Sucursales.CrearSucursal;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Cuerpo del alta de sucursal. Las propiedades son anulables para que los faltantes los valide FluentValidation.</summary>
public sealed record CrearSucursalPeticion(
    string? Nombre,
    string? Direccion,
    string? Telefono,
    string? Slug,
    string? WhatsApp,
    string? PhoneNumberId,
    string? Prefijo,
    string? ZonaHoraria);

[ApiController]
[Route("api/r/{slug}/sucursales")]
[RequiereRol(Rol.AdministradorGeneral)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")] // Restaurante no disponible
public sealed class SucursalesController(IMediator mediador) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SucursalDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<SucursalDto>> Crear(string slug, CrearSucursalPeticion peticion, CancellationToken ct)
    {
        var datos = new DatosSucursalDto(
            peticion.Nombre ?? string.Empty,
            peticion.Direccion ?? string.Empty,
            peticion.Telefono ?? string.Empty,
            peticion.WhatsApp ?? string.Empty,
            // Un phoneNumberId vacío equivale a no enviarlo.
            string.IsNullOrWhiteSpace(peticion.PhoneNumberId) ? null : peticion.PhoneNumberId,
            peticion.Prefijo ?? string.Empty,
            peticion.ZonaHoraria);
        var sucursal = await mediador.Send(new CrearSucursalCommand(peticion.Slug ?? string.Empty, datos), ct);
        return Created($"/api/r/{slug.ToLowerInvariant()}/sucursales/{sucursal.Id}", sucursal);
    }
}
