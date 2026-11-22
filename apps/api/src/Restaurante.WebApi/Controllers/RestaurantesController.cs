using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Restaurantes;
using Restaurante.Application.Features.Restaurantes.CrearRestaurante;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Cuerpo del alta de restaurante. Las propiedades son anulables para que los faltantes los valide FluentValidation.</summary>
public sealed record CrearRestaurantePeticion(
    string? Nombre,
    string? Slug,
    PaletaDto? Paleta,
    AdministradorInicialDto? Administrador);

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
}
