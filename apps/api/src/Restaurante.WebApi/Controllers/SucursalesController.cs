using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Sucursales;
using Restaurante.Application.Features.Sucursales.CambiarEstadoSucursal;
using Restaurante.Application.Features.Sucursales.CrearSucursal;
using Restaurante.Application.Features.Sucursales.EditarSucursal;
using Restaurante.Application.Features.Sucursales.ListarSucursales;
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

/// <summary>Cuerpo de la edición de sucursal: igual que el alta pero sin slug (inmutable; si llega se ignora).</summary>
public sealed record EditarSucursalPeticion(
    string? Nombre,
    string? Direccion,
    string? Telefono,
    string? WhatsApp,
    string? PhoneNumberId,
    string? Prefijo,
    string? ZonaHoraria);

public sealed record CambiarEstadoSucursalPeticion(bool? Activa);

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
        var datos = ADatos(
            peticion.Nombre, peticion.Direccion, peticion.Telefono, peticion.WhatsApp,
            peticion.PhoneNumberId, peticion.Prefijo, peticion.ZonaHoraria);
        var sucursal = await mediador.Send(new CrearSucursalCommand(peticion.Slug ?? string.Empty, datos), ct);
        return Created($"/api/r/{slug.ToLowerInvariant()}/sucursales/{sucursal.Id}", sucursal);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SucursalDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SucursalDto>>> Listar(CancellationToken ct) =>
        Ok(await mediador.Send(new ListarSucursalesQuery(), ct));

    /// <summary>Edita los datos de la sucursal; un slug enviado en el cuerpo se ignora.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<SucursalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<SucursalDto>> Editar(string slug, Guid id, EditarSucursalPeticion peticion, CancellationToken ct)
    {
        var datos = ADatos(
            peticion.Nombre, peticion.Direccion, peticion.Telefono, peticion.WhatsApp,
            peticion.PhoneNumberId, peticion.Prefijo, peticion.ZonaHoraria);
        return Ok(await mediador.Send(new EditarSucursalCommand(id, datos), ct));
    }

    [HttpPut("{id:guid}/estado")]
    [ProducesResponseType<SucursalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<SucursalDto>> CambiarEstado(string slug, Guid id, CambiarEstadoSucursalPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new CambiarEstadoSucursalCommand(id, peticion.Activa), ct));

    private static DatosSucursalDto ADatos(
        string? nombre, string? direccion, string? telefono, string? whatsApp,
        string? phoneNumberId, string? prefijo, string? zonaHoraria) =>
        new(
            nombre ?? string.Empty,
            direccion ?? string.Empty,
            telefono ?? string.Empty,
            whatsApp ?? string.Empty,
            // Un phoneNumberId vacío equivale a no enviarlo.
            string.IsNullOrWhiteSpace(phoneNumberId) ? null : phoneNumberId,
            prefijo ?? string.Empty,
            zonaHoraria);
}
