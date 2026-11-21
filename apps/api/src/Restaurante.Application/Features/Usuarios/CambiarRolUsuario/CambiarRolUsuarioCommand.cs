using MediatR;

namespace Restaurante.Application.Features.Usuarios.CambiarRolUsuario;

public sealed record CambiarRolUsuarioCommand(Guid Id, string Rol) : IRequest<UsuarioDto>;
