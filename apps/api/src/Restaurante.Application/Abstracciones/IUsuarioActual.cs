using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Abstracciones;

/// <summary>
/// Usuario autenticado que ejecuta la operación en curso. Sus datos son los vigentes en base de datos (los fija
/// el middleware de sesión al validar el sello), no los del token (004/RF-13).
/// </summary>
public interface IUsuarioActual
{
    Guid Id { get; }

    Rol Rol { get; }

    /// <summary>Sucursales asignadas al usuario; vacío para los administradores general y de plataforma.</summary>
    IReadOnlyCollection<Guid> Sucursales { get; }
}
