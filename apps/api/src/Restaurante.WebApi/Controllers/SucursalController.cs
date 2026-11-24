using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Sucursales.Horario;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Cuerpo del guardado de horario. <c>Dias</c> es anulable para que el validador informe de su ausencia.</summary>
public sealed record GuardarHorarioPeticion(IReadOnlyList<HorarioDiaDto>? Dias);

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
}
