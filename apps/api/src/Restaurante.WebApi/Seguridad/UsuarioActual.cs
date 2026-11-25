using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.WebApi.Seguridad;

/// <summary>
/// Usuario autenticado de la petición actual (scoped). Lo fija <see cref="MiddlewareSesionUsuario"/> con los datos
/// de base de datos tras validar el sello del token (004/RF-13); leerlo sin usuario fijado es un error de programación.
/// </summary>
public sealed class UsuarioActual : IUsuarioActual
{
    private Usuario? _usuario;

    public Guid Id => Usuario.Id;

    public Rol Rol => Usuario.Rol;

    public IReadOnlyCollection<Guid> Sucursales => Usuario.Sucursales;

    private Usuario Usuario =>
        _usuario ?? throw new InvalidOperationException("No hay un usuario autenticado en la petición actual.");

    public void Establecer(Usuario usuario) => _usuario = usuario;
}
