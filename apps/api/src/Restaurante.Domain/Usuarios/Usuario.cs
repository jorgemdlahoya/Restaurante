namespace Restaurante.Domain.Usuarios;

public sealed class Usuario
{
    private readonly List<UsuarioSucursal> _sucursales = [];

    // Constructor para EF.
    private Usuario()
    {
        NombreUsuario = string.Empty;
        NombreUsuarioNormalizado = string.Empty;
        HashContrasena = string.Empty;
    }

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
        SelloSesion = Guid.NewGuid();
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

    /// <summary>Se renueva al cortar sesiones: desactivar, cambiar contraseña, rol o sucursales (004/RF-13).</summary>
    public Guid SelloSesion { get; private set; }

    public IReadOnlyCollection<Guid> Sucursales => _sucursales.Select(s => s.SucursalId).ToList();

    public static Usuario Crear(
        Guid? restauranteId,
        string nombreUsuario,
        string hashContrasena,
        Rol rol,
        IReadOnlyCollection<Guid> sucursales,
        DateTimeOffset creadoEn)
    {
        ValidarRestaurante(rol, restauranteId);
        ValidarSinDuplicados(sucursales);
        ValidarSucursales(rol, sucursales.Count);

        var usuario = new Usuario(Guid.NewGuid(), restauranteId, nombreUsuario, hashContrasena, rol, creadoEn);
        usuario.ReemplazarSucursales(sucursales);
        return usuario;
    }

    /// <summary>
    /// Cardinalidad rol ↔ sucursales (004/RF-02): administradores general y de plataforma 0, administrador de
    /// sucursal exactamente 1, cajero y cocinero al menos 1.
    /// </summary>
    public static bool CardinalidadValida(Rol rol, int cantidad) => rol switch
    {
        Rol.AdministradorGeneral or Rol.AdministradorPlataforma => cantidad == 0,
        Rol.AdministradorSucursal => cantidad == 1,
        _ => cantidad >= 1,
    };

    /// <summary>Lanza <see cref="ArgumentException"/> si no se cumple <see cref="CardinalidadValida"/>.</summary>
    public static void ValidarSucursales(Rol rol, int cantidad)
    {
        if (!CardinalidadValida(rol, cantidad))
        {
            throw new ArgumentException(
                $"El rol {rol} no admite {cantidad} sucursales.", nameof(cantidad));
        }
    }

    /// <summary>
    /// Asigna rol y sucursales; devuelve si cambió algo. Sólo entonces renueva el sello. Como en <see cref="Crear"/>,
    /// sólo el administrador de plataforma carece de restaurante (004/RF-04).
    /// </summary>
    public bool AsignarRolYSucursales(Rol rol, IReadOnlyCollection<Guid> sucursales)
    {
        ValidarRestaurante(rol, RestauranteId);
        ValidarSinDuplicados(sucursales);
        ValidarSucursales(rol, sucursales.Count);

        var mismoConjunto = sucursales.ToHashSet().SetEquals(_sucursales.Select(s => s.SucursalId));
        if (rol == Rol && mismoConjunto)
        {
            return false;
        }

        Rol = rol;
        ReemplazarSucursales(sucursales);
        RenovarSello();
        return true;
    }

    public void CambiarContrasena(string hash)
    {
        HashContrasena = hash;
        RenovarSello();
    }

    public void Desactivar()
    {
        Activo = false;
        RenovarSello();
    }

    public void Reactivar() => Activo = true;

    private void RenovarSello() => SelloSesion = Guid.NewGuid();

    private static void ValidarRestaurante(Rol rol, Guid? restauranteId)
    {
        if ((rol == Rol.AdministradorPlataforma) != (restauranteId is null))
        {
            throw new ArgumentException(
                "Sólo el administrador de plataforma carece de restaurante.", nameof(restauranteId));
        }
    }

    private void ReemplazarSucursales(IReadOnlyCollection<Guid> sucursales)
    {
        _sucursales.Clear();
        if (RestauranteId is { } restauranteId)
        {
            _sucursales.AddRange(sucursales.Select(s => new UsuarioSucursal(Id, s, restauranteId)));
        }
    }

    private static void ValidarSinDuplicados(IReadOnlyCollection<Guid> sucursales)
    {
        if (sucursales.Distinct().Count() != sucursales.Count)
        {
            throw new ArgumentException("Las sucursales no pueden repetirse.", nameof(sucursales));
        }
    }
}
