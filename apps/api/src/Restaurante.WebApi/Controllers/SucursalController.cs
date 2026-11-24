using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Sucursales.Cierres;
using Restaurante.Application.Features.Sucursales.Horario;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Cuerpo del guardado de horario. <c>Dias</c> es anulable para que el validador informe de su ausencia.</summary>
public sealed record GuardarHorarioPeticion(IReadOnlyList<HorarioDiaDto>? Dias);

/// <summary>Cuerpo de la creación de un cierre. <c>Fin</c> es anulable para que el validador informe de su ausencia.</summary>
public sealed record CrearCierrePeticion(string? Inicio, string? Fin, string? Motivo);

/// <summary>Recursos de una sucursal concreta; el middleware fija su contexto desde la ruta.</summary>
[ApiController]
[Route("api/r/{slug}/s/{sucursal}")]
[RequiereRol(Rol.AdministradorGeneral)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")] // Sucursal no disponible
public sealed class SucursalController(IMediator mediador) : ControllerBase
{
    [HttpGet("horario")]
    [ProducesResponseType<IReadOnlyList<HorarioDiaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HorarioDiaDto>>> ObtenerHorario(CancellationToken ct) =>
        Ok(await mediador.Send(new ObtenerHorarioQuery(), ct));

    [HttpPut("horario")]
    [ProducesResponseType<IReadOnlyList<HorarioDiaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<HorarioDiaDto>>> GuardarHorario(GuardarHorarioPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new GuardarHorarioCommand(peticion.Dias ?? []), ct));

    [HttpGet("cierres")]
    [ProducesResponseType<IReadOnlyList<CierreTemporalDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CierreTemporalDto>>> ListarCierres(CancellationToken ct) =>
        Ok(await mediador.Send(new ListarCierresQuery(), ct));

    [HttpPost("cierres")]
    [ProducesResponseType<CierreTemporalDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<CierreTemporalDto>> CrearCierre(CrearCierrePeticion peticion, CancellationToken ct)
    {
        var cierre = await mediador.Send(new CrearCierreCommand(peticion.Inicio, peticion.Fin ?? string.Empty, peticion.Motivo), ct);
        return Created($"/api/r/{RouteData.Values["slug"]}/s/{RouteData.Values["sucursal"]}/cierres/{cierre.Id}", cierre);
    }

    [HttpPost("cierres/{id:guid}/cancelar")]
    [ProducesResponseType<CierreTemporalDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CierreTemporalDto>> CancelarCierre(Guid id, CancellationToken ct) =>
        Ok(await mediador.Send(new CancelarCierreCommand(id), ct));
}
