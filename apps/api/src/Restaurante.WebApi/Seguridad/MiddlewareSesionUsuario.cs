using Restaurante.Application.Abstracciones;
using Restaurante.Infrastructure.Seguridad;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Seguridad;

/// <summary>
/// Valida la sesión de toda petición autenticada (004/RF-13, RF-14). Va después de <c>UseAuthentication()</c> y antes
/// de <see cref="MiddlewareContextoRestaurante"/>, también en <c>/api/plataforma…</c>. Carga al usuario del claim
/// <c>sub</c> por clave primaria (sin filtro de restaurante) y responde 401 "Sesión no válida" si el token no trae
/// <c>sello</c>, el usuario no existe, está inactivo o su sello actual es otro. Si todo cuadra, fija
/// <see cref="UsuarioActual"/> con los datos de base de datos. Sin autenticar no hace nada.
/// </summary>
public sealed class MiddlewareSesionUsuario(RequestDelegate siguiente)
{
    public const string TituloSesionInvalida = "Sesión no válida";
    public const string DetalleSesionInvalida = "Vuelve a iniciar sesión.";

    public async Task InvokeAsync(HttpContext contexto, UsuarioActual usuarioActual, IUsuarioRepository usuarios)
    {
        if (contexto.User.Identity?.IsAuthenticated != true)
        {
            await siguiente(contexto);
            return;
        }

        // Un token anterior al sello (o con valores ilegibles) se trata igual que uno caducado: 401, nunca 500.
        var idLegible = Guid.TryParse(contexto.User.FindFirst("sub")?.Value, out var id);
        var selloLegible = Guid.TryParse(contexto.User.FindFirst(EmisorTokensJwt.ClaimSello)?.Value, out var sello);
        var usuario = idLegible && selloLegible ? await usuarios.ObtenerParaSesionAsync(id, contexto.RequestAborted) : null;
        if (usuario is not { Activo: true } || usuario.SelloSesion != sello)
        {
            // RFC 6750 §3: un 401 con esquema Bearer debe anunciar el desafío.
            contexto.Response.Headers.WWWAuthenticate = "Bearer";
            await RespuestasProblema.EscribirAsync(
                contexto, StatusCodes.Status401Unauthorized, TituloSesionInvalida, DetalleSesionInvalida);
            return;
        }

        usuarioActual.Establecer(usuario);
        await siguiente(contexto);
    }
}
