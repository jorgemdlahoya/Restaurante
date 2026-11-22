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
/// <item><c>/api/r/{slug}…</c>: el slug (sin distinguir mayúsculas) debe ser de un restaurante activo; si no, 404.</item>
/// <item>Resto: el claim <c>restaurant_id</c> del token, si lo hay (provisional; T-08 completa esta regla).</item>
/// </list>
/// </summary>
public sealed partial class MiddlewareContextoRestaurante(RequestDelegate siguiente)
{
    public const string TituloNoDisponible = "Restaurante no disponible";
    public const string DetalleNoDisponible = "La dirección no corresponde a un restaurante activo.";

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
        if (coincidencia.Success)
        {
            var slug = coincidencia.Groups[1].Value.ToLowerInvariant();
            var restaurante = await restaurantes.ObtenerPorSlugAsync(slug, contexto.RequestAborted);
            if (restaurante is not { Activo: true })
            {
                await RespuestasProblema.EscribirAsync(
                    contexto, StatusCodes.Status404NotFound, TituloNoDisponible, DetalleNoDisponible);
                return;
            }

            contextoRestaurante.Establecer(restaurante.Id);
        }
        else if (Guid.TryParse(contexto.User.FindFirst(EmisorTokensJwt.ClaimRestaurante)?.Value, out var restauranteId))
        {
            // Provisional hasta T-08 (rutas /api/r/{slug}/usuarios, 401 si el restaurante está inactivo).
            contextoRestaurante.Establecer(restauranteId);
        }

        await siguiente(contexto);
    }

    [GeneratedRegex("^/api/r/([^/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex RutaRestaurante();
}
