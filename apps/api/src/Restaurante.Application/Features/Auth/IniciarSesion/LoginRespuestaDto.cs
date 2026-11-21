namespace Restaurante.Application.Features.Auth.IniciarSesion;

public sealed record LoginRespuestaDto(string Token, DateTimeOffset ExpiraEn, string NombreUsuario, string Rol);
