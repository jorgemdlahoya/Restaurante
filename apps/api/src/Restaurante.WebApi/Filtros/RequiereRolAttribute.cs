using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Restaurante.Application.Comun;
using Restaurante.Domain.Usuarios;

namespace Restaurante.WebApi.Filtros;

/// <summary>
/// Exige un JWT válido cuyo claim <c>role</c> (en minúsculas) coincida con alguno de los roles permitidos.
/// Sin autenticar responde 401; autenticado con otro rol, 403; ambos con <see cref="ProblemDetails"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiereRolAttribute(params Rol[] roles) : Attribute, IAuthorizationFilter
{
    private readonly HashSet<string> _permitidos = roles.Select(r => r.ComoTexto()).ToHashSet();

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var usuario = context.HttpContext.User;

        if (usuario.Identity?.IsAuthenticated != true)
        {
            context.Result = Problema(
                StatusCodes.Status401Unauthorized, "No autenticado", "Se requiere un token válido.");
            return;
        }

        var rol = usuario.FindFirst("role")?.Value;
        if (rol is null || !_permitidos.Contains(rol))
        {
            context.Result = Problema(
                StatusCodes.Status403Forbidden, "Acceso denegado", "No tiene permisos para esta operación.");
        }
    }

    private static ObjectResult Problema(int estado, string titulo, string detalle) =>
        new(new ProblemDetails { Status = estado, Title = titulo, Detail = detalle })
        {
            StatusCode = estado,
            ContentTypes = { "application/problem+json" },
        };
}
