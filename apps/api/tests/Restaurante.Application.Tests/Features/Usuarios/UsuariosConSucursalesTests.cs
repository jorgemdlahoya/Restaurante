using FluentValidation;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Usuarios;
using Restaurante.Application.Features.Usuarios.CrearUsuario;
using Restaurante.Application.Features.Usuarios.ListarUsuarios;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Usuarios;

/// <summary>Alta y lista de usuarios con sucursales y alcance (004/RF-01..RF-03, RF-07, RF-08, RF-11, RF-26).</summary>
public class UsuariosConSucursalesTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly UsuarioRepositoryEnMemoria _usuarios;
    private readonly SucursalRepositoryEnMemoria _sucursales;
    private readonly RelojFijo _reloj = new();
    private readonly Sucursal _centro;
    private readonly Sucursal _norte;

    public UsuariosConSucursalesTests()
    {
        _usuarios = new(_contexto);
        _sucursales = new(_contexto);
        _norte = Sembrar("norte", "Norte", "N", "+525511111111");
        _centro = Sembrar("centro", "Centro", "C", "+525522222222");
    }

    private Sucursal Sembrar(string slug, string nombre, string prefijo, string whatsApp)
    {
        var sucursal = Sucursal.Crear(
            RestauranteId, slug,
            new DatosSucursal(nombre, "Calle 1", "+525500000000", whatsApp, null, prefijo, ZonasHorarias.PorDefecto),
            _reloj.Ahora);
        _sucursales.Sucursales.Add(sucursal);
        return sucursal;
    }

    private Usuario Usuario_(string nombre, Rol rol, params Sucursal[] sucursales)
    {
        var usuario = Usuario.Crear(
            RestauranteId, nombre, "hash:x", rol, sucursales.Select(s => s.Id).ToList(), _reloj.Ahora);
        _usuarios.Agregar(usuario);
        return usuario;
    }

    private ResolutorActor Resolutor(Usuario actor) =>
        new(new UsuarioActualFalso(actor.Id, actor.Rol, actor.Sucursales), _sucursales);

    private CrearUsuarioHandler Crear(Usuario actor) =>
        new(_usuarios, new HasherFalso(), _reloj, _contexto, Resolutor(actor));

    private static CrearUsuarioCommand Alta(string rol, params string[] sucursales) =>
        new("pepe", "secreta123", rol, sucursales);

    [Fact(DisplayName = "004/RF-01: el administrador general crea un cajero con sus sucursales ordenadas por nombre")]
    public async Task General_crea_cajero_con_sucursales()
    {
        var jefa = Usuario_("jefa", Rol.AdministradorGeneral);

        var dto = await Crear(jefa).Handle(Alta("cajero", "norte", "centro"), CancellationToken.None);

        var guardado = _usuarios.Usuarios.Single(u => u.NombreUsuario == "pepe");
        Assert.Equal(new[] { _centro.Id, _norte.Id }.Order(), guardado.Sucursales.Order());
        Assert.Equal(
            [new SucursalAsignadaDto("centro", "Centro", true), new SucursalAsignadaDto("norte", "Norte", true)],
            dto.Sucursales);
        Assert.True(dto.Gestionable);
    }

    [Fact(DisplayName = "004/RF-02: una sucursal inexistente lanza ValidationException en sucursales sin crear nada")]
    public async Task Sucursal_inexistente_lanza_validacion()
    {
        var jefa = Usuario_("jefa", Rol.AdministradorGeneral);

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            Crear(jefa).Handle(Alta("cajero", "centro", "fantasma"), CancellationToken.None));

        var fallo = Assert.Single(error.Errors);
        Assert.Equal("Sucursales", fallo.PropertyName);
        Assert.Equal("No existe la sucursal fantasma", fallo.ErrorMessage);
        Assert.DoesNotContain(_usuarios.Usuarios, u => u.NombreUsuario == "pepe");
    }

    [Fact(DisplayName = "004/RF-08: el administrador de sucursal crea un cocinero en su sucursal")]
    public async Task Administrador_de_sucursal_crea_cocinero()
    {
        var admin = Usuario_("admin", Rol.AdministradorSucursal, _centro);

        var dto = await Crear(admin).Handle(Alta("cocinero", "CENTRO"), CancellationToken.None);

        Assert.Equal([new SucursalAsignadaDto("centro", "Centro", true)], dto.Sucursales);
        Assert.True(dto.Gestionable);
    }

    [Theory(DisplayName = "004/RF-08: el administrador de sucursal fuera de alcance recibe ProhibidoException, aunque la sucursal no exista")]
    [InlineData("cocinero", new[] { "norte" })]
    [InlineData("cocinero", new[] { "fantasma" })]
    [InlineData("cajero", new[] { "centro", "fantasma" })]
    [InlineData("administrador_sucursal", new[] { "centro" })]
    [InlineData("administrador_general", new string[0])]
    public async Task Administrador_de_sucursal_fuera_de_alcance(string rol, string[] sucursales)
    {
        var admin = Usuario_("admin", Rol.AdministradorSucursal, _centro);

        await Assert.ThrowsAsync<ProhibidoException>(() =>
            Crear(admin).Handle(Alta(rol, sucursales), CancellationToken.None));

        Assert.DoesNotContain(_usuarios.Usuarios, u => u.NombreUsuario == "pepe");
    }

    [Fact(DisplayName = "004/RF-26: el administrador de sucursal con su sucursal inactiva no puede operar")]
    public async Task Sucursal_inactiva_no_opera()
    {
        var admin = Usuario_("admin", Rol.AdministradorSucursal, _centro);
        _centro.Desactivar();

        await Assert.ThrowsAsync<ProhibidoException>(() => Resolutor(admin).ObtenerAsync(CancellationToken.None));
    }

    [Theory(DisplayName = "004/RF-11: cajero y cocinero no pueden operar sobre usuarios")]
    [InlineData(Rol.Cajero)]
    [InlineData(Rol.Cocinero)]
    public async Task Personal_operativo_no_opera(Rol rol)
    {
        var personal = Usuario_("personal", rol, _centro);

        await Assert.ThrowsAsync<ProhibidoException>(() => Resolutor(personal).ObtenerAsync(CancellationToken.None));
        await Assert.ThrowsAsync<ProhibidoException>(() =>
            new ListarUsuariosHandler(_usuarios, Resolutor(personal)).Handle(new ListarUsuariosQuery(), CancellationToken.None));
    }

    [Fact(DisplayName = "004/RF-03: el administrador general lista todos con sus sucursales, activas o no")]
    public async Task General_lista_todos()
    {
        var jefa = Usuario_("jefa", Rol.AdministradorGeneral);
        Usuario_("Zoe", Rol.Cajero, _norte);
        Usuario_("beto", Rol.Cocinero, _norte, _centro);
        _norte.Desactivar();

        var lista = await new ListarUsuariosHandler(_usuarios, Resolutor(jefa))
            .Handle(new ListarUsuariosQuery(), CancellationToken.None);

        Assert.Equal(["beto", "jefa", "Zoe"], lista.Select(u => u.NombreUsuario));
        Assert.Equal(
            [new SucursalAsignadaDto("centro", "Centro", true), new SucursalAsignadaDto("norte", "Norte", false)],
            lista[0].Sucursales);
        Assert.Empty(lista[1].Sucursales);
        Assert.All(lista, u => Assert.True(u.Gestionable));
    }

    [Fact(DisplayName = "004/RF-07: el administrador de sucursal sólo lista a su personal, con gestionable")]
    public async Task Administrador_de_sucursal_lista_su_personal()
    {
        Usuario_("jefa", Rol.AdministradorGeneral);
        var admin = Usuario_("admin", Rol.AdministradorSucursal, _centro);
        Usuario_("ana", Rol.Cajero, _centro);
        Usuario_("compartido", Rol.Cajero, _centro, _norte);
        Usuario_("luis", Rol.Cocinero, _norte);

        var lista = await new ListarUsuariosHandler(_usuarios, Resolutor(admin))
            .Handle(new ListarUsuariosQuery(), CancellationToken.None);

        Assert.Equal(["admin", "ana", "compartido"], lista.Select(u => u.NombreUsuario));
        Assert.Equal([false, true, false], lista.Select(u => u.Gestionable));
    }

    [Theory(DisplayName = "004/RF-02: el validador aplica la cardinalidad y los duplicados en sucursales")]
    [InlineData("administrador_general", new[] { "centro" }, "El administrador general no lleva sucursales")]
    [InlineData("administrador_sucursal", new string[0], "El administrador de sucursal debe tener exactamente una sucursal")]
    [InlineData("administrador_sucursal", new[] { "centro", "norte" }, "El administrador de sucursal debe tener exactamente una sucursal")]
    [InlineData("cajero", new string[0], "El cajero y el cocinero deben tener al menos una sucursal")]
    [InlineData("cocinero", new[] { "centro", "CENTRO" }, "Las sucursales no pueden repetirse")]
    [InlineData("cocinero", new[] { "centro", "" }, "Cada sucursal se indica por su slug")]
    public void Validador_rechaza_sucursales_invalidas(string rol, string[] sucursales, string mensaje)
    {
        var resultado = new CrearUsuarioValidator().Validate(Alta(rol, sucursales));

        var fallo = Assert.Single(resultado.Errors);
        Assert.Equal("Sucursales", fallo.PropertyName);
        Assert.Equal(mensaje, fallo.ErrorMessage);
    }

    [Theory(DisplayName = "004/RF-02: el validador acepta la cardinalidad de cada rol")]
    [InlineData("administrador_general", new string[0])]
    [InlineData("administrador_sucursal", new[] { "centro" })]
    [InlineData("cajero", new[] { "centro", "norte" })]
    [InlineData("cocinero", new[] { "centro" })]
    public void Validador_acepta_sucursales_validas(string rol, string[] sucursales)
    {
        Assert.True(new CrearUsuarioValidator().Validate(Alta(rol, sucursales)).IsValid);
    }
}
