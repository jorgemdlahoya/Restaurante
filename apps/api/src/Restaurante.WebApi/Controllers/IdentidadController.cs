using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Restaurantes.ObtenerIdentidad;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Identidad pública del restaurante (nombre, paleta, logo) para pintar la pantalla de login.</summary>
[ApiController]
[Route("api/r/{slug}")]
public sealed class IdentidadController(IMediator mediador) : ControllerBase
{
    [HttpGet("identidad")]
    [EndpointAnonimo]
    [ProducesResponseType<IdentidadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IdentidadDto>> Obtener(CancellationToken ct) =>
        Ok(await mediador.Send(new ObtenerIdentidadQuery(), ct));
}
