namespace Restaurante.Domain.Usuarios;

public sealed class Usuario
{
    private Usuario(
        Guid id, Guid? restauranteId, string nombreUsuario, string hashContrasena, Rol rol, DateTimeOffset creadoEn)
    {
        Id = id;
        RestauranteId = restauranteId;
        NombreUsuario = nombreUsuario;
        NombreUsuarioNormalizado = nombreUsuario.ToLowerInvariant();
        HashContrasena = hashContrasena;
        Rol = rol;
        Activo = true;
        CreadoEn = creadoEn;
    }

    public Guid Id { get; private set; }

    /// <summary>Restaurante al que pertenece; <c>null</c> sólo para el administrador de plataforma.</summary>
    public Guid? RestauranteId { get; private set; }

    public string NombreUsuario { get; private set; }
    public string NombreUsuarioNormalizado { get; private set; }
    public string HashContrasena { get; private set; }
    public Rol Rol { get; private set; }
    public bool Activo { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }

    public static Usuario Crear(
        Guid? restauranteId, string nombreUsuario, string hashContrasena, Rol rol, DateTimeOffset creadoEn)
    {
        if ((rol == Rol.AdministradorPlataforma) != (restauranteId is null))
        {
            throw new ArgumentException(
                "Sólo el administrador de plataforma carece de restaurante.", nameof(restauranteId));
        }

        return new(Guid.NewGuid(), restauranteId, nombreUsuario, hashContrasena, rol, creadoEn);
    }

    public void Desactivar() => Activo = false;

    public void Reactivar() => Activo = true;

    public void CambiarRol(Rol nuevo) => Rol = nuevo;
}
