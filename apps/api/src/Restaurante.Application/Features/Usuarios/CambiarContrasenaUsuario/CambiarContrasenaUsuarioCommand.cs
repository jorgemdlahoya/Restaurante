using MediatR;

namespace Restaurante.Application.Features.Usuarios.CambiarContrasenaUsuario;

/// <summary>Asigna una contraseña nueva a un usuario (004/RF-06). El controlador envía vacío si falta (Art. 7.3).</summary>
public sealed record CambiarContrasenaUsuarioCommand(Guid Id, string Contrasena) : IRequest<Unit>;
