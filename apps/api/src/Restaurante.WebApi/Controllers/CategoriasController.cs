using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Categorias;
using Restaurante.Application.Features.Categorias.CambiarEstadoCategoria;
using Restaurante.Application.Features.Categorias.CrearCategoria;
using Restaurante.Application.Features.Categorias.EditarCategoria;
using Restaurante.Application.Features.Categorias.EliminarCategoria;
using Restaurante.Application.Features.Categorias.ListarCategorias;
using Restaurante.Application.Features.Categorias.ObtenerCategoria;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Cuerpo del alta de categoría. Las propiedades son anulables para que los faltantes los valide FluentValidation.</summary>
public sealed record CrearCategoriaPeticion(
    string? Nombre,
    string? NombreCorto,
    // MVC lee JSON con las opciones Web (un "3" entre comillas se aceptaría como número): el orden es sólo numérico.
    [property: JsonNumberHandling(JsonNumberHandling.Strict)] int? Orden);

/// <summary>Cuerpo de la edición de categoría; mismas reglas que el alta.</summary>
public sealed record EditarCategoriaPeticion(
    string? Nombre,
    string? NombreCorto,
    [property: JsonNumberHandling(JsonNumberHandling.Strict)] int? Orden);

public sealed record CambiarEstadoCategoriaPeticion(bool? Activa);

[ApiController]
[Route("api/r/{slug}/categorias")]
[RequiereRol(Rol.AdministradorGeneral)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")] // Restaurante no disponible
public sealed class CategoriasController(IMediator mediador) : ControllerBase
{
    private const int PaginaPorDefecto = 1;
    private const int TamanoPorDefecto = 20;

    [HttpGet]
    [ProducesResponseType<PaginaCategoriasDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PaginaCategoriasDto>> Listar(
        [FromQuery] int? pagina, [FromQuery] int? tamano, CancellationToken ct) =>
        Ok(await mediador.Send(
            new ListarCategoriasQuery(pagina ?? PaginaPorDefecto, tamano ?? TamanoPorDefecto), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategoriaDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CategoriaDto>> Obtener(string slug, Guid id, CancellationToken ct) =>
        Ok(await mediador.Send(new ObtenerCategoriaQuery(id), ct));

    [HttpPost]
    [ProducesResponseType<CategoriaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CategoriaDto>> Crear(string slug, CrearCategoriaPeticion peticion, CancellationToken ct)
    {
        var categoria = await mediador.Send(
            new CrearCategoriaCommand(new DatosCategoriaDto(peticion.Nombre, peticion.NombreCorto, peticion.Orden)), ct);
        return Created($"/api/r/{slug.ToLowerInvariant()}/categorias/{categoria.Id}", categoria);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CategoriaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CategoriaDto>> Editar(string slug, Guid id, EditarCategoriaPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(
            new EditarCategoriaCommand(id, new DatosCategoriaDto(peticion.Nombre, peticion.NombreCorto, peticion.Orden)), ct));

    [HttpPut("{id:guid}/estado")]
    [ProducesResponseType<CategoriaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<CategoriaDto>> CambiarEstado(string slug, Guid id, CambiarEstadoCategoriaPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new CambiarEstadoCategoriaCommand(id, peticion.Activa), ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")] // Con platillos
    public async Task<IActionResult> Eliminar(string slug, Guid id, CancellationToken ct)
    {
        await mediador.Send(new EliminarCategoriaCommand(id), ct);
        return NoContent();
    }
}
