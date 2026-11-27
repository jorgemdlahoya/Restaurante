using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Platillos;
using Restaurante.Application.Features.Platillos.CrearPlatillo;
using Restaurante.Application.Features.Platillos.ListarPlatillos;
using Restaurante.Application.Features.Platillos.ObtenerPlatillo;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Cuerpo del alta de platillo. Las propiedades son anulables para que los faltantes los valide FluentValidation.</summary>
public sealed record CrearPlatilloPeticion(
    string? Nombre,
    string? NombreCorto,
    string? Descripcion,
    // MVC lee JSON con las opciones Web (un "3" entre comillas se aceptaría como número): el orden es sólo numérico.
    [property: JsonNumberHandling(JsonNumberHandling.Strict)] int? Orden,
    IReadOnlyList<string?>? Ingredientes);

[ApiController]
[Route("api/r/{slug}")]
[RequiereRol(Rol.AdministradorGeneral)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")] // Restaurante o categoría no disponibles
public sealed class PlatillosController(IMediator mediador) : ControllerBase
{
    private const int PaginaPorDefecto = 1;
    private const int TamanoPorDefecto = 20;

    [HttpGet("categorias/{categoriaId:guid}/platillos")]
    [ProducesResponseType<PaginaPlatillosDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PaginaPlatillosDto>> Listar(
        string slug, Guid categoriaId, [FromQuery] int? pagina, [FromQuery] int? tamano, CancellationToken ct) =>
        Ok(await mediador.Send(
            new ListarPlatillosQuery(categoriaId, pagina ?? PaginaPorDefecto, tamano ?? TamanoPorDefecto), ct));

    [HttpGet("platillos/{id:guid}")]
    [ProducesResponseType<PlatilloDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatilloDto>> Obtener(string slug, Guid id, CancellationToken ct) =>
        Ok(await mediador.Send(new ObtenerPlatilloQuery(id), ct));

    [HttpPost("categorias/{categoriaId:guid}/platillos")]
    [ProducesResponseType<PlatilloDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<PlatilloDto>> Crear(
        string slug, Guid categoriaId, CrearPlatilloPeticion peticion, CancellationToken ct)
    {
        var platillo = await mediador.Send(
            new CrearPlatilloCommand(categoriaId, new DatosPlatilloDto(
                peticion.Nombre, peticion.NombreCorto, peticion.Descripcion, peticion.Orden, peticion.Ingredientes)), ct);
        return Created($"/api/r/{slug.ToLowerInvariant()}/platillos/{platillo.Id}", platillo);
    }
}
