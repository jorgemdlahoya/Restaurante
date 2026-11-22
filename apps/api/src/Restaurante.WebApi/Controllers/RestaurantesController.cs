using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Restaurantes;
using Restaurante.Application.Features.Restaurantes.CambiarEstadoRestaurante;
using Restaurante.Application.Features.Restaurantes.CrearRestaurante;
using Restaurante.Application.Features.Restaurantes.EditarRestaurante;
using Restaurante.Application.Features.Restaurantes.ListarRestaurantes;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Cuerpo del alta de restaurante. Las propiedades son anulables para que los faltantes los valide FluentValidation.</summary>
public sealed record CrearRestaurantePeticion(
    string? Nombre,
    string? Slug,
    PaletaDto? Paleta,
    AdministradorInicialDto? Administrador);

/// <summary>Cuerpo de la edición de restaurante. Cualquier otro campo (p. ej. slug) se ignora: el slug es inmutable.</summary>
public sealed record EditarRestaurantePeticion(string? Nombre, PaletaDto? Paleta);

[ApiController]
[Route("api/plataforma/restaurantes")]
[RequiereRol(Rol.AdministradorPlataforma)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class RestaurantesController(IMediator mediador) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RestauranteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<RestauranteDto>> Crear(CrearRestaurantePeticion peticion, CancellationToken ct)
    {
        var restaurante = await mediador.Send(new CrearRestauranteCommand(
            peticion.Nombre ?? string.Empty,
            peticion.Slug ?? string.Empty,
            peticion.Paleta,
            new AdministradorInicialDto(
                peticion.Administrador?.NombreUsuario ?? string.Empty,
                peticion.Administrador?.Contrasena ?? string.Empty)), ct);
        return Created($"/api/plataforma/restaurantes/{restaurante.Id}", restaurante);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RestauranteDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RestauranteDto>>> Listar(CancellationToken ct) =>
        Ok(await mediador.Send(new ListarRestaurantesQuery(), ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType<RestauranteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<RestauranteDto>> Editar(Guid id, EditarRestaurantePeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new EditarRestauranteCommand(id, peticion.Nombre ?? string.Empty, peticion.Paleta), ct));

    [HttpPut("{id:guid}/estado")]
    [ProducesResponseType<RestauranteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<RestauranteDto>> CambiarEstado(Guid id, CambiarEstadoPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new CambiarEstadoRestauranteCommand(id, peticion.Activo), ct));
}
