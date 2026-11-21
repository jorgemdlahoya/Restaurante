using Restaurante.Application.Abstracciones;

namespace Restaurante.WebApi.Seguridad;

/// <summary>Lee el identificador del usuario autenticado del claim <c>sub</c> del JWT.</summary>
public sealed class UsuarioActual(IHttpContextAccessor accesor) : IUsuarioActual
{
    public Guid Id
    {
        get
        {
            var sub = accesor.HttpContext?.User.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id)
                ? id
                : throw new InvalidOperationException("No hay un usuario autenticado en la petición actual.");
        }
    }
}
