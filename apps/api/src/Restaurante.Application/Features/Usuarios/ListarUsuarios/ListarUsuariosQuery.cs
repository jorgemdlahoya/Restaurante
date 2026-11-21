using MediatR;

namespace Restaurante.Application.Features.Usuarios.ListarUsuarios;

public sealed record ListarUsuariosQuery : IRequest<IReadOnlyList<UsuarioDto>>;
