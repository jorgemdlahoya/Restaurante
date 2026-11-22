using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Usuarios;
using Restaurante.Application.Features.Usuarios.CambiarEstadoUsuario;
using Restaurante.Application.Features.Usuarios.CambiarRolUsuario;
using Restaurante.Application.Features.Usuarios.CrearUsuario;
using Restaurante.Application.Features.Usuarios.ListarUsuarios;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>Cuerpo del alta de usuario. Las propiedades son anulables para que los faltantes los valide FluentValidation.</summary>
public sealed record CrearUsuarioPeticion(string? NombreUsuario, string? Contrasena, string? Rol);

/// <summary>Cuerpo del cambio de estado (activar/desactivar).</summary>
public sealed record CambiarEstadoPeticion(bool? Activo);

/// <summary>Cuerpo del cambio de rol.</summary>
public sealed record CambiarRolPeticion(string? Rol);

[ApiController]
[Route("api/usuarios")]
[RequiereRol(Rol.AdministradorGeneral)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class UsuariosController(IMediator mediador) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UsuarioDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar(CancellationToken ct) =>
        Ok(await mediador.Send(new ListarUsuariosQuery(), ct));

    [HttpPost]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> Crear(CrearUsuarioPeticion peticion, CancellationToken ct)
    {
        var usuario = await mediador.Send(new CrearUsuarioCommand(
            peticion.NombreUsuario ?? string.Empty,
            peticion.Contrasena ?? string.Empty,
            peticion.Rol ?? string.Empty), ct);
        return Created($"/api/usuarios/{usuario.Id}", usuario);
    }

    [HttpPut("{id:guid}/estado")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> CambiarEstado(Guid id, CambiarEstadoPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new CambiarEstadoUsuarioCommand(id, peticion.Activo), ct));

    [HttpPut("{id:guid}/rol")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> CambiarRol(Guid id, CambiarRolPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new CambiarRolUsuarioCommand(id, peticion.Rol ?? string.Empty), ct));
}
