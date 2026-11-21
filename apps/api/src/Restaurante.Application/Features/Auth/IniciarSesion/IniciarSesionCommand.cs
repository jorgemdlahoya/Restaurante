using MediatR;

namespace Restaurante.Application.Features.Auth.IniciarSesion;

public sealed record IniciarSesionCommand(string NombreUsuario, string Contrasena) : IRequest<LoginRespuestaDto>;
