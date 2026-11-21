using MediatR;

namespace Restaurante.Application.Features.Usuarios.CambiarEstadoUsuario;

public sealed record CambiarEstadoUsuarioCommand(Guid Id, bool Activo) : IRequest<UsuarioDto>;
