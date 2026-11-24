using MediatR;

namespace Restaurante.Application.Features.Usuarios.CrearUsuario;

/// <summary><see cref="Sucursales"/> son slugs de sucursales del restaurante (004/RF-01).</summary>
public sealed record CrearUsuarioCommand(
    string NombreUsuario, string Contrasena, string Rol, IReadOnlyList<string> Sucursales) : IRequest<UsuarioDto>;
