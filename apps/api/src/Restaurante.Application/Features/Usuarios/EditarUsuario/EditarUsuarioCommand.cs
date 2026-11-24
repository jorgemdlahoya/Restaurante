using MediatR;

namespace Restaurante.Application.Features.Usuarios.EditarUsuario;

/// <summary>
/// Edición de rol y sucursales de un usuario (004/RF-04); <see cref="Sucursales"/> son slugs. El nombre de usuario
/// no se modifica.
/// </summary>
public sealed record EditarUsuarioCommand(Guid Id, string Rol, IReadOnlyList<string> Sucursales) : IRequest<UsuarioDto>;
