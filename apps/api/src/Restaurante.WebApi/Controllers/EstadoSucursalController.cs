using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Sucursales.EstadoAtencion;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Estado de atención público de una sucursal (sin token); los middlewares fijan restaurante y sucursal desde la ruta.</summary>
[ApiController]
[Route("api/r/{slug}/s/{sucursal}")]
public sealed class EstadoSucursalController(IMediator mediador) : ControllerBase
{
    [HttpGet("estado")]
    [EndpointAnonimo]
    [ProducesResponseType<EstadoAtencionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")] // Sucursal no disponible
    public async Task<ActionResult<EstadoAtencionDto>> Obtener(CancellationToken ct) =>
        Ok(await mediador.Send(new ObtenerEstadoAtencionQuery(), ct));
}
