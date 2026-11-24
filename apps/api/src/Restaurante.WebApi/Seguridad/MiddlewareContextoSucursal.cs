using System.Text.RegularExpressions;
using Restaurante.Application.Abstracciones;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Seguridad;

/// <summary>
/// Fija el <see cref="ContextoSucursal"/> de la petición. Va después de <see cref="MiddlewareContextoRestaurante"/>:
/// en <c>/api/r/{slug}/s/{sucursal}…</c> (sin distinguir mayúsculas) busca la sucursal dentro del restaurante
/// ya fijado (el repositorio aplica el filtro de restaurante). Si no existe en ese restaurante, 404
/// "Sucursal no disponible" (nunca 403); si existe, activa o no, la fija. Fuera de esa ruta no hay sucursal.
/// </summary>
public sealed partial class MiddlewareContextoSucursal(RequestDelegate siguiente)
{
    public const string TituloNoDisponible = "Sucursal no disponible";
    public const string DetalleNoDisponible = "La dirección no corresponde a una sucursal de este restaurante.";

    public async Task InvokeAsync(
        HttpContext contexto,
        ContextoSucursal contextoSucursal,
        IContextoRestaurante contextoRestaurante,
        ISucursalRepository sucursales)
    {
        var coincidencia = RutaSucursal().Match(contexto.Request.Path.Value ?? string.Empty);
        if (coincidencia.Success && contextoRestaurante.RestauranteId is not null)
        {
            var slug = coincidencia.Groups[1].Value.ToLowerInvariant();
            var sucursal = await sucursales.ObtenerPorSlugAsync(slug, contexto.RequestAborted);
            if (sucursal is null)
            {
                await RespuestasProblema.EscribirAsync(
                    contexto, StatusCodes.Status404NotFound, TituloNoDisponible, DetalleNoDisponible);
                return;
            }

            contextoSucursal.Establecer(sucursal.Id);
        }

        await siguiente(contexto);
    }

    [GeneratedRegex("^/api/r/[^/]+/s/([^/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex RutaSucursal();
}
