using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Comun;

public static class RolExtensiones
{
    private static readonly Dictionary<string, Rol> PorTexto =
        Enum.GetValues<Rol>().ToDictionary(ComoTexto, StringComparer.Ordinal);

    /// <summary>Texto en snake_case del rol (API, JWT y base de datos), tal como lo fija la constitución.</summary>
    public static string ComoTexto(this Rol rol) => rol switch
    {
        Rol.AdministradorPlataforma => "administrador_plataforma",
        Rol.AdministradorGeneral => "administrador_general",
        Rol.AdministradorSucursal => "administrador_sucursal",
        Rol.Cajero => "cajero",
        Rol.Cocinero => "cocinero",
        _ => throw new ArgumentOutOfRangeException(nameof(rol), rol, "Rol desconocido")
    };

    /// <summary>Lee un rol por coincidencia exacta con su texto; cualquier otra grafía no es un rol.</summary>
    public static bool TryDesdeTexto(string? texto, out Rol rol)
    {
        rol = default;
        return texto is not null && PorTexto.TryGetValue(texto, out rol);
    }

    public static Rol DesdeTexto(string texto) =>
        TryDesdeTexto(texto, out var rol)
            ? rol
            : throw new ArgumentException($"Rol desconocido: {texto}", nameof(texto));
}
