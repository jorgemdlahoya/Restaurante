using System.Runtime.CompilerServices;
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

    // Hash producido por el propio hasher (mismo coste que uno real), calculado una vez por hasher.
    private static readonly ConditionalWeakTable<IHasherContrasenas, string> HashesFicticios = new();

    private static string HashFicticio(IHasherContrasenas hasher) =>
        HashesFicticios.GetValue(hasher, h => h.Hashear(Guid.NewGuid().ToString("N")));

    public async Task<LoginRespuestaDto> Handle(IniciarSesionCommand request, CancellationToken cancellationToken)
    {
        var normalizado = request.NombreUsuario.Trim().ToLowerInvariant();
        var usuario = await usuarios.ObtenerPorNombreNormalizadoAsync(normalizado, cancellationToken);

        // Siempre se ejecuta Verificar (contra un hash ficticio si el usuario no existe) y se
        // comprueba la contraseña antes que Activo, para no filtrar existencia/estado por tiempo.
        var hashAComparar = usuario?.HashContrasena ?? HashFicticio(hasher);
        var contrasenaCorrecta = hasher.Verificar(hashAComparar, request.Contrasena);

        if (usuario is null || !contrasenaCorrecta || !usuario.Activo)
        {
            throw new CredencialesInvalidasException(MensajeCredenciales);
        }

        var token = emisor.Emitir(usuario);
        return new LoginRespuestaDto(token.Token, token.ExpiraEn, usuario.NombreUsuario,
            usuario.Rol.ComoTexto());
    }
}
