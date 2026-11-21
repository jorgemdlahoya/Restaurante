namespace Restaurante.Domain.Usuarios;

public sealed class Usuario
{
    private Usuario(Guid id, string nombreUsuario, string hashContrasena, Rol rol, DateTimeOffset creadoEn)
    {
        Id = id;
        NombreUsuario = nombreUsuario;
        NombreUsuarioNormalizado = nombreUsuario.ToLowerInvariant();
        HashContrasena = hashContrasena;
        Rol = rol;
        Activo = true;
        CreadoEn = creadoEn;
    }

    public Guid Id { get; private set; }
    public string NombreUsuario { get; private set; }
    public string NombreUsuarioNormalizado { get; private set; }
    public string HashContrasena { get; private set; }
    public Rol Rol { get; private set; }
    public bool Activo { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }

    public static Usuario Crear(string nombreUsuario, string hashContrasena, Rol rol, DateTimeOffset creadoEn) =>
        new(Guid.NewGuid(), nombreUsuario, hashContrasena, rol, creadoEn);

    public void Desactivar() => Activo = false;

    public void Reactivar() => Activo = true;

    public void CambiarRol(Rol nuevo) => Rol = nuevo;
}
