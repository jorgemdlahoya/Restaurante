using MediatR;

namespace Restaurante.Application.Features.Usuarios.CambiarEstadoUsuario;

/// <summary><see cref="Activo"/> es anulable para que su ausencia la rechace el validador (Art. 7.3).</summary>
public sealed record CambiarEstadoUsuarioCommand(Guid Id, bool? Activo) : IRequest<UsuarioDto>;
