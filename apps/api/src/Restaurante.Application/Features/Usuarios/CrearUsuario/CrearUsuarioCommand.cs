using MediatR;

namespace Restaurante.Application.Features.Usuarios.CrearUsuario;

public sealed record CrearUsuarioCommand(string NombreUsuario, string Contrasena, string Rol) : IRequest<UsuarioDto>;
