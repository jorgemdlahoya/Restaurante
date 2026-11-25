using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Auth.MiSesion;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Datos vigentes de la sesión del usuario autenticado (004/RF-16).</summary>
[ApiController]
[Route("api/r/{slug}/mi-sesion")]
[RequiereRol(Rol.AdministradorGeneral, Rol.AdministradorSucursal, Rol.Cajero, Rol.Cocinero)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")] // Restaurante no disponible
public sealed class MiSesionController(IMediator mediador) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MiSesionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MiSesionDto>> Obtener(CancellationToken ct) =>
        Ok(await mediador.Send(new ObtenerMiSesionQuery(), ct));
}
