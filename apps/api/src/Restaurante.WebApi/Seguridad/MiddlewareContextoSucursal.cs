using System.Text.RegularExpressions;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Seguridad;

/// <summary>
/// Fija el <see cref="ContextoSucursal"/> de la petición. Va después de <see cref="MiddlewareContextoRestaurante"/>:
/// en <c>/api/r/{slug}/s/{sucursal}…</c> (sin distinguir mayúsculas) busca la sucursal dentro del restaurante
/// ya fijado (el repositorio aplica el filtro de restaurante). Si no existe en ese restaurante, 404
/// "Sucursal no disponible" (nunca 403); si existe, activa o no, la fija. Con sesión, un rol distinto del administrador
/// general sólo accede a sus sucursales asignadas y activas (004/RF-15, RF-25). Fuera de esa ruta no hay sucursal.
/// </summary>
public sealed partial class MiddlewareContextoSucursal(RequestDelegate siguiente)
{
    public const string TituloNoDisponible = "Sucursal no disponible";
    public const string DetalleNoDisponible = "La dirección no corresponde a una sucursal de este restaurante.";

    public async Task InvokeAsync(
        HttpContext contexto,
        ContextoSucursal contextoSucursal,
        IContextoRestaurante contextoRestaurante,
        ISucursalRepository sucursales,
        IUsuarioActual usuarioActual)
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

            // 004/RF-15, RF-25: con sesión, todo rol salvo el administrador general sólo entra a sus sucursales
            // asignadas y activas; las demás se ven como inexistentes (404, nunca 403: Art. 12.5). Sin sesión no
            // cambia nada. Con sesión, MiddlewareSesionUsuario ya fijó al usuario (si no, respondió 401).
            if (contexto.User.Identity?.IsAuthenticated == true
                && usuarioActual.Rol != Rol.AdministradorGeneral
                && (!sucursal.Activa || !usuarioActual.Sucursales.Contains(sucursal.Id)))
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
