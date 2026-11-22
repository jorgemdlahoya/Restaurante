using System.Text.RegularExpressions;
using Restaurante.Application.Abstracciones;
using Restaurante.Infrastructure.Seguridad;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Seguridad;

/// <summary>
/// Fija el <see cref="ContextoRestaurante"/> de la petición. Va después de <c>UseAuthentication()</c>.
/// Reglas, en orden:
/// <list type="number">
/// <item><c>/api/plataforma…</c>: ámbito de plataforma, aunque venga un token de restaurante.</item>
/// <item>Token autenticado con <c>restaurant_id</c>: el restaurante debe existir y estar activo (si no, 401); en
/// <c>/api/r/{slug}…</c> el slug (sin distinguir mayúsculas) debe ser el suyo (si no, 404).</item>
/// <item><c>/api/r/{slug}…</c> sin restaurante en el token: el slug debe ser de un restaurante activo; si no, 404.</item>
/// <item>Resto: ámbito de plataforma.</item>
/// </list>
/// </summary>
public sealed partial class MiddlewareContextoRestaurante(RequestDelegate siguiente)
{
    public const string TituloNoDisponible = "Restaurante no disponible";
    public const string DetalleNoDisponible = "La dirección no corresponde a un restaurante activo.";
    public const string TituloNoAutenticado = "No autenticado";
    public const string DetalleRestauranteInactivo = "El restaurante no está activo.";

    public async Task InvokeAsync(
        HttpContext contexto, ContextoRestaurante contextoRestaurante, IRestauranteRepository restaurantes)
    {
        var ruta = contexto.Request.Path;

        if (ruta.StartsWithSegments("/api/plataforma", StringComparison.OrdinalIgnoreCase))
        {
            await siguiente(contexto);
            return;
        }

        var coincidencia = RutaRestaurante().Match(ruta.Value ?? string.Empty);
        var slug = coincidencia.Success ? coincidencia.Groups[1].Value.ToLowerInvariant() : null;

        var claim = contexto.User.Identity?.IsAuthenticated == true
            ? contexto.User.FindFirst(EmisorTokensJwt.ClaimRestaurante)?.Value
            : null;
        if (claim is not null)
        {
            var restauranteDelToken = Guid.TryParse(claim, out var id)
                ? await restaurantes.ObtenerPorIdAsync(id, contexto.RequestAborted)
                : null;
            if (restauranteDelToken is not { Activo: true })
            {
                // RFC 6750 §3: un 401 con esquema Bearer debe anunciar el desafío.
                contexto.Response.Headers.WWWAuthenticate = "Bearer";
                await RespuestasProblema.EscribirAsync(
                    contexto, StatusCodes.Status401Unauthorized, TituloNoAutenticado, DetalleRestauranteInactivo);
                return;
            }

            if (slug is not null && slug != restauranteDelToken.Slug)
            {
                await EscribirNoDisponibleAsync(contexto);
                return;
            }

            contextoRestaurante.Establecer(restauranteDelToken.Id);
        }
        else if (slug is not null)
        {
            var restaurante = await restaurantes.ObtenerPorSlugAsync(slug, contexto.RequestAborted);
            if (restaurante is not { Activo: true })
            {
                await EscribirNoDisponibleAsync(contexto);
                return;
            }

            contextoRestaurante.Establecer(restaurante.Id);
        }

        await siguiente(contexto);
    }

    private static Task EscribirNoDisponibleAsync(HttpContext contexto) =>
        RespuestasProblema.EscribirAsync(
            contexto, StatusCodes.Status404NotFound, TituloNoDisponible, DetalleNoDisponible);

    [GeneratedRegex("^/api/r/([^/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex RutaRestaurante();
}
