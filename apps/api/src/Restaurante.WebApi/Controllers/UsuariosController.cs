using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Features.Usuarios;
using Restaurante.Application.Features.Usuarios.CambiarEstadoUsuario;
using Restaurante.Application.Features.Usuarios.CrearUsuario;
using Restaurante.Application.Features.Usuarios.EditarUsuario;
using Restaurante.Application.Features.Usuarios.ListarUsuarios;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Controllers;

/// <summary>
/// Cuerpo del alta de usuario. Las propiedades son anulables para que los faltantes los valide FluentValidation.
/// <see cref="Sucursales"/> son slugs; ausente equivale a ninguna.
/// </summary>
public sealed record CrearUsuarioPeticion(string? NombreUsuario, string? Contrasena, string? Rol, string[]? Sucursales);

/// <summary>Cuerpo del cambio de estado (activar/desactivar).</summary>
public sealed record CambiarEstadoPeticion(bool? Activo);

/// <summary>
/// Cuerpo de la edición de rol y sucursales (004/RF-04). <see cref="Sucursales"/> son slugs; ausente equivale a
/// ninguna. El nombre de usuario no se edita: un <c>nombreUsuario</c> en el cuerpo se ignora.
/// </summary>
public sealed record EditarUsuarioPeticion(string? Rol, string[]? Sucursales);

[ApiController]
[Route("api/r/{slug}/usuarios")]
// El alcance fino (qué usuarios ve y gestiona el administrador de sucursal) lo deciden los handlers (004/RF-07..RF-11).
[RequiereRol(Rol.AdministradorGeneral, Rol.AdministradorSucursal)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")] // Restaurante no disponible
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
    public async Task<ActionResult<UsuarioDto>> Crear(string slug, CrearUsuarioPeticion peticion, CancellationToken ct)
    {
        var usuario = await mediador.Send(new CrearUsuarioCommand(
            peticion.NombreUsuario ?? string.Empty,
            peticion.Contrasena ?? string.Empty,
            peticion.Rol ?? string.Empty,
            peticion.Sucursales ?? []), ct);
        return Created($"/api/r/{slug.ToLowerInvariant()}/usuarios/{usuario.Id}", usuario);
    }

    [HttpPut("{id:guid}/estado")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> CambiarEstado(Guid id, CambiarEstadoPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new CambiarEstadoUsuarioCommand(id, peticion.Activo), ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> Editar(Guid id, EditarUsuarioPeticion peticion, CancellationToken ct) =>
        Ok(await mediador.Send(new EditarUsuarioCommand(id, peticion.Rol ?? string.Empty, peticion.Sucursales ?? []), ct));
}
