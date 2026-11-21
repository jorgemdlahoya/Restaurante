using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Auth.IniciarSesion;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Cuerpo de la petición de inicio de sesión.</summary>
public sealed record LoginPeticion(string? NombreUsuario, string? Contrasena);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediador) : ControllerBase
{
    [HttpPost("login")]
    [EndpointAnonimo]
    [ProducesResponseType<LoginRespuestaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<LoginRespuestaDto>> Login(LoginPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new IniciarSesionCommand(peticion.NombreUsuario ?? string.Empty, peticion.Contrasena ?? string.Empty), ct));
}
