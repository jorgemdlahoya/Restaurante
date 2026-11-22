using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Auth.IniciarSesion;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>
/// Login de plataforma. El middleware nunca fija restaurante en <c>/api/plataforma</c> (aunque llegue un token de
/// restaurante), así que sólo existen los usuarios sin restaurante.
/// </summary>
[ApiController]
[Route("api/plataforma/auth")]
public sealed class PlataformaAuthController(IMediator mediador) : ControllerBase
{
    [HttpPost("login")]
    [EndpointAnonimo]
    [ProducesResponseType<LoginRespuestaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<LoginRespuestaDto>> Login(LoginPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new IniciarSesionCommand(peticion.NombreUsuario ?? string.Empty, peticion.Contrasena ?? string.Empty), ct));
}
