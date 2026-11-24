using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Auth.IniciarSesion;
using Restaurante.Application.Features.Auth.MiSesion;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Auth;

/// <summary>Mi sesión y login sin sucursales activas (004/RF-16, RF-17).</summary>
public class MiSesionTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly UsuarioRepositoryEnMemoria _usuarios;
    private readonly SucursalRepositoryEnMemoria _sucursales;
    private readonly HasherFalso _hasher = new();
    private int _contadorWhatsApp;

    public MiSesionTests()
    {
        _usuarios = new UsuarioRepositoryEnMemoria(_contexto);
        _sucursales = new SucursalRepositoryEnMemoria(_contexto);
    }

    private Sucursal Sembrar(string slug, string nombre, bool activa = true)
    {
        var sucursal = SucursalesDePrueba.Crear(
            RestauranteId, slug, nombre, slug[..1].ToUpperInvariant(), $"+5255000{++_contadorWhatsApp:D5}");
        if (!activa)
        {
            sucursal.Desactivar();
        }

        _sucursales.Sucursales.Add(sucursal);
        return sucursal;
    }

    private Usuario AgregarUsuario(string nombre, Rol rol, params Sucursal[] sucursales)
    {
        var usuario = Usuario.Crear(
            RestauranteId, nombre, _hasher.Hashear("secreta123"), rol, sucursales.Select(s => s.Id).ToList(),
            new RelojFijo().Ahora);
        _usuarios.Agregar(usuario);
        return usuario;
    }

    private Task<MiSesionDto> MiSesionAsync(Usuario usuario) =>
        new ObtenerMiSesionHandler(new UsuarioActualFalso(usuario.Id, usuario.Rol, usuario.Sucursales), _usuarios, _sucursales)
            .Handle(new ObtenerMiSesionQuery(), CancellationToken.None);

    [Fact(DisplayName = "004/RF-16: mi-sesion del cajero trae sólo sus sucursales activas ordenadas por nombre")]
    public async Task CajeroTraeSusSucursalesActivasOrdenadas()
    {
        var zeta = Sembrar("zeta", "Zeta");
        var alfa = Sembrar("alfa", "Alfa");
        var inactiva = Sembrar("beta", "Beta", activa: false);
        Sembrar("gamma", "Gamma");
        var usuario = AgregarUsuario("caja", Rol.Cajero, zeta, alfa, inactiva);

        var sesion = await MiSesionAsync(usuario);

        Assert.Equal("caja", sesion.NombreUsuario);
        Assert.Equal("cajero", sesion.Rol);
        Assert.Equal(["alfa", "zeta"], sesion.Sucursales.Select(s => s.Slug));
        Assert.Equal(["Alfa", "Zeta"], sesion.Sucursales.Select(s => s.Nombre));
    }

    [Fact(DisplayName = "004/RF-16: mi-sesion del administrador general trae todas las sucursales activas")]
    public async Task GeneralTraeTodasLasActivas()
    {
        Sembrar("norte", "Norte");
        Sembrar("centro", "Centro");
        Sembrar("viejo", "Viejo", activa: false);
        var usuario = AgregarUsuario("dueno", Rol.AdministradorGeneral);

        var sesion = await MiSesionAsync(usuario);

        Assert.Equal(["centro", "norte"], sesion.Sucursales.Select(s => s.Slug));
    }

    [Fact(DisplayName = "004/RF-16: mi-sesion conserva el orden por nombre que da el repositorio")]
    public async Task ConservaElOrdenDelRepositorio()
    {
        // El doble devuelve la lista tal cual: simula el orden de la base de datos (acentos y minúsculas
        // intercalados), que difiere del ordinal ("Centro" < "bosque" < "Ávila").
        var avila = SucursalesDePrueba.Crear(RestauranteId, "avila", "Ávila", "A", "+525500000001");
        var bosque = SucursalesDePrueba.Crear(RestauranteId, "bosque", "bosque", "B", "+525500000002");
        var centro = SucursalesDePrueba.Crear(RestauranteId, "centro", "Centro", "C", "+525500000003");
        var usuario = AgregarUsuario("caja", Rol.Cajero, avila, bosque, centro);
        var repositorio = new SucursalesEnOrdenFijo([avila, bosque, centro]);

        var sesion = await new ObtenerMiSesionHandler(
                new UsuarioActualFalso(usuario.Id, usuario.Rol, usuario.Sucursales), _usuarios, repositorio)
            .Handle(new ObtenerMiSesionQuery(), CancellationToken.None);

        Assert.Equal(["avila", "bosque", "centro"], sesion.Sucursales.Select(s => s.Slug));
    }

    /// <summary>Sólo <see cref="ListarAsync"/>: devuelve las sucursales en el orden recibido, sin reordenar.</summary>
    private sealed class SucursalesEnOrdenFijo(IReadOnlyList<Sucursal> lista) : ISucursalRepository
    {
        public Task<IReadOnlyList<Sucursal>> ListarAsync(CancellationToken ct) => Task.FromResult(lista);

        public Task<Sucursal?> ObtenerPorIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();

        public Task<Sucursal?> ObtenerPorSlugAsync(string slug, CancellationToken ct) => throw new NotSupportedException();

        public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<bool> ExisteSlugAsync(string slug, CancellationToken ct) => throw new NotSupportedException();

        public Task<bool> ExistePrefijoAsync(string prefijo, Guid? excluirId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<bool> ExisteWhatsAppAsync(string whatsApp, Guid? excluirId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<bool> ExistePhoneNumberIdAsync(string phoneNumberId, Guid? excluirId, CancellationToken ct) =>
            throw new NotSupportedException();

        public void Agregar(Sucursal sucursal, IReadOnlyList<HorarioDia> horario) => throw new NotSupportedException();

        public Task<IReadOnlyList<HorarioDia>> ObtenerHorarioAsync(CancellationToken ct) => throw new NotSupportedException();

        public Task GuardarCambiosAsync(CancellationToken ct) => throw new NotSupportedException();
    }

    private IniciarSesionHandler Login() => new(_usuarios, _hasher, new EmisorTokensFalso(), _sucursales);

    [Fact(DisplayName = "004/RF-17: un cajero con todas sus sucursales inactivas recibe SinSucursalesActivasException")]
    public async Task CajeroSinSucursalesActivasNoRecibeToken()
    {
        AgregarUsuario("caja", Rol.Cajero, Sembrar("centro", "Centro", activa: false));

        await Assert.ThrowsAsync<SinSucursalesActivasException>(() =>
            Login().Handle(new IniciarSesionCommand("caja", "secreta123"), CancellationToken.None));
    }

    [Fact(DisplayName = "004/RF-17: con contraseña incorrecta sigue respondiendo credenciales inválidas")]
    public async Task ContrasenaIncorrectaSigueSiendoCredencialesInvalidas()
    {
        AgregarUsuario("caja", Rol.Cajero, Sembrar("centro", "Centro", activa: false));

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            Login().Handle(new IniciarSesionCommand("caja", "otra-clave"), CancellationToken.None));
    }

    [Fact(DisplayName = "004/RF-17: el administrador general inicia sesión aunque no haya sucursales activas")]
    public async Task GeneralIniciaSesionSinSucursales()
    {
        AgregarUsuario("dueno", Rol.AdministradorGeneral);

        var respuesta = await Login().Handle(new IniciarSesionCommand("dueno", "secreta123"), CancellationToken.None);

        Assert.Equal("administrador_general", respuesta.Rol);
    }

    [Fact(DisplayName = "004/RF-17: un cajero con al menos una sucursal activa inicia sesión")]
    public async Task CajeroConUnaActivaIniciaSesion()
    {
        AgregarUsuario("caja", Rol.Cajero, Sembrar("centro", "Centro", activa: false), Sembrar("norte", "Norte"));

        var respuesta = await Login().Handle(new IniciarSesionCommand("caja", "secreta123"), CancellationToken.None);

        Assert.Equal("cajero", respuesta.Rol);
    }
}
