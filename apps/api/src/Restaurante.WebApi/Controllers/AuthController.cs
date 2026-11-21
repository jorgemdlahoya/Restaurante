using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Auth.IniciarSesion;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Cuerpo de la petición de inicio de sesión.</summary>
public sealed record LoginPeticion(string NombreUsuario, string Contrasena);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediador) : ControllerBase
{
    [HttpPost("login")]
    [EndpointAnonimo]
    [ProducesResponseType<LoginRespuestaDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginRespuestaDto>> Login(LoginPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new IniciarSesionCommand(peticion.NombreUsuario, peticion.Contrasena), ct));
}
