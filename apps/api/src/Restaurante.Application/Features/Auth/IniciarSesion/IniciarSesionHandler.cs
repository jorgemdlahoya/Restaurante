using System.Runtime.CompilerServices;
using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Auth.IniciarSesion;

public sealed class IniciarSesionHandler(
    IUsuarioRepository usuarios,
    IHasherContrasenas hasher,
    IEmisorTokens emisor,
    ISucursalRepository sucursales) : IRequestHandler<IniciarSesionCommand, LoginRespuestaDto>
{
    private const string MensajeCredenciales = "Usuario o contraseña incorrectos";
    private const string MensajeSinSucursales = "No tienes sucursales activas. Habla con tu administrador.";

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

        // Sólo tras credenciales válidas y usuario activo (no adelanta información); los administradores general y
        // de plataforma no tienen sucursales asignadas y quedan fuera (004/RF-17).
        if (usuario.Rol is Rol.AdministradorSucursal or Rol.Cajero or Rol.Cocinero
            && !await TieneSucursalActivaAsync(usuario, cancellationToken))
        {
            throw new SinSucursalesActivasException(MensajeSinSucursales);
        }

        var token = emisor.Emitir(usuario);
        return new LoginRespuestaDto(token.Token, token.ExpiraEn, usuario.NombreUsuario,
            usuario.Rol.ComoTexto());
    }

    private async Task<bool> TieneSucursalActivaAsync(Usuario usuario, CancellationToken ct)
    {
        var asignadas = usuario.Sucursales;
        var todas = await sucursales.ListarAsync(ct);
        return todas.Any(s => s.Activa && asignadas.Contains(s.Id));
    }
}
