using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Comun;

public static class RolExtensiones
{
    /// <summary>Representación en minúsculas del rol que se expone al cliente.</summary>
    public static string ComoTexto(this Rol rol) => rol.ToString().ToLowerInvariant();
}
