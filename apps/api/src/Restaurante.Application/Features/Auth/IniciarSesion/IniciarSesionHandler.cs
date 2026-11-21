using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.Application.Features.Auth.IniciarSesion;

public sealed class IniciarSesionHandler(
    IUsuarioRepository usuarios,
    IHasherContrasenas hasher,
    IEmisorTokens emisor) : IRequestHandler<IniciarSesionCommand, LoginRespuestaDto>
{
    private const string MensajeCredenciales = "Usuario o contraseña incorrectos";

    public async Task<LoginRespuestaDto> Handle(IniciarSesionCommand request, CancellationToken cancellationToken)
    {
        var normalizado = request.NombreUsuario.Trim().ToLowerInvariant();
        var usuario = await usuarios.ObtenerPorNombreNormalizadoAsync(normalizado, cancellationToken);

        if (usuario is null || !usuario.Activo || !hasher.Verificar(usuario.HashContrasena, request.Contrasena))
        {
            throw new CredencialesInvalidasException(MensajeCredenciales);
        }

        var token = emisor.Emitir(usuario);
        return new LoginRespuestaDto(token.Token, token.ExpiraEn, usuario.NombreUsuario,
            usuario.Rol.ComoTexto());
    }
}
