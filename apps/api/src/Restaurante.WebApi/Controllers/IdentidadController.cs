using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Restaurante.Application.Features.Restaurantes.ObtenerLogo;
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

    /// <summary>Logo del restaurante con ETag; <c>no-cache</c> obliga a revalidar y <c>If-None-Match</c> igual da 304.</summary>
    [HttpGet("logo")]
    [EndpointAnonimo]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Logo(CancellationToken ct)
    {
        var logo = await mediador.Send(new ObtenerLogoQuery(), ct);
        var etag = new EntityTagHeaderValue($"\"{logo.ETag}\"");
        var cabeceras = Response.GetTypedHeaders();
        cabeceras.ETag = etag;
        cabeceras.CacheControl = new CacheControlHeaderValue { Public = true, NoCache = true };

        var coincide = Request.GetTypedHeaders().IfNoneMatch
            .Any(e => e.Equals(EntityTagHeaderValue.Any) || e.Compare(etag, useStrongComparison: false));
        return coincide ? StatusCode(StatusCodes.Status304NotModified) : File(logo.Contenido, logo.Tipo);
    }
}
