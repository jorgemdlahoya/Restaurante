using FluentValidation;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Usuarios;
using Restaurante.Application.Features.Usuarios.EditarUsuario;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Usuarios;

/// <summary>
/// Edición de rol y sucursales (004/RF-04, RF-05, RF-09, RF-10, RF-13). Sustituye a las pruebas de
/// <c>CambiarRolUsuario</c> (001/RF-16, RF-17, RF-19), que se conservan aquí sobre el nuevo caso de uso.
/// </summary>
public class EditarUsuarioTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly UsuarioRepositoryEnMemoria _usuarios;
    private readonly SucursalRepositoryEnMemoria _sucursales;
    private readonly RelojFijo _reloj = new();
    private readonly Sucursal _centro;
    private readonly Sucursal _norte;

    public EditarUsuarioTests()
    {
        _usuarios = new(_contexto);
        _sucursales = new(_contexto);
        _norte = Sembrar("norte", "Norte", "N", "+525511111111");
        _centro = Sembrar("centro", "Centro", "C", "+525522222222");
    }

    private Sucursal Sembrar(string slug, string nombre, string prefijo, string whatsApp)
    {
        var sucursal = SucursalesDePrueba.Crear(RestauranteId, slug, nombre, prefijo, whatsApp);
        _sucursales.Sucursales.Add(sucursal);
        return sucursal;
    }

    private Usuario Agregar(string nombre, Rol rol, params Sucursal[] sucursales)
    {
        var usuario = Usuario.Crear(
            RestauranteId, nombre, "hash:x", rol, sucursales.Select(s => s.Id).ToList(), _reloj.Ahora);
        _usuarios.Agregar(usuario);
        return usuario;
    }

    /// <summary>Ejecuta como <paramref name="actor"/>; sin actor, como un administrador general ajeno al almacén.</summary>
    private Task<UsuarioDto> Ejecutar(Guid id, string rol, string[] sucursales, Usuario? actor = null)
    {
        var actual = actor is null
            ? new UsuarioActualFalso(Guid.NewGuid())
            : new UsuarioActualFalso(actor.Id, actor.Rol, actor.Sucursales);
        return new EditarUsuarioHandler(_usuarios, new ResolutorActor(actual, _sucursales))
            .Handle(new EditarUsuarioCommand(id, rol, sucursales), CancellationToken.None);
    }

    [Fact(DisplayName = "001/RF-16: cambia el rol")]
    public async Task CambiaElRol()
    {
        var u = Agregar("pepe", Rol.Cajero, _centro);

        var dto = await Ejecutar(u.Id, "cocinero", ["centro"]);

        Assert.Equal("cocinero", dto.Rol);
        Assert.Equal(Rol.Cocinero, u.Rol);
        Assert.Equal(1, _usuarios.GuardadosLlamados);
    }

    [Fact(DisplayName = "001/RF-16: valida que el rol sea uno del conjunto exacto")]
    public void ValidaRol()
    {
        var validador = new EditarUsuarioValidator();

        Assert.True(validador.Validate(new EditarUsuarioCommand(Guid.NewGuid(), "cocinero", ["centro"])).IsValid);
        Assert.False(validador.Validate(new EditarUsuarioCommand(Guid.NewGuid(), "Cajero", ["centro"])).IsValid);
        Assert.False(validador.Validate(new EditarUsuarioCommand(Guid.NewGuid(), "chef", ["centro"])).IsValid);
        Assert.False(validador.Validate(
            new EditarUsuarioCommand(Guid.NewGuid(), "administrador_plataforma", [])).IsValid);
    }

    [Theory(DisplayName = "004/RF-04: el validador aplica la cardinalidad de sucursales de RF-02")]
    [InlineData("administrador_general", new[] { "centro" }, "El administrador general no lleva sucursales")]
    [InlineData("administrador_sucursal", new string[0], "El administrador de sucursal debe tener exactamente una sucursal")]
    [InlineData("administrador_sucursal", new[] { "centro", "norte" }, "El administrador de sucursal debe tener exactamente una sucursal")]
    [InlineData("cajero", new string[0], "El cajero y el cocinero deben tener al menos una sucursal")]
    [InlineData("cocinero", new[] { "centro", "CENTRO" }, "Las sucursales no pueden repetirse")]
    [InlineData("cocinero", new[] { " " }, "Cada sucursal se indica por su slug")]
    public void ValidaCardinalidad(string rol, string[] sucursales, string mensaje)
    {
        var resultado = new EditarUsuarioValidator().Validate(new EditarUsuarioCommand(Guid.NewGuid(), rol, sucursales));

        var error = Assert.Single(resultado.Errors);
        Assert.Equal("Sucursales", error.PropertyName);
        Assert.Equal(mensaje, error.ErrorMessage);
    }

    [Fact(DisplayName = "004/RF-04: el administrador general cambia rol y sucursales")]
    public async Task General_cambia_rol_y_sucursales()
    {
        var u = Agregar("pepe", Rol.Cajero, _centro);
        var sello = u.SelloSesion;

        var dto = await Ejecutar(u.Id, "cocinero", ["NORTE", "centro"]);

        Assert.Equal("cocinero", dto.Rol);
        Assert.Equal(
            [new SucursalAsignadaDto("centro", "Centro", true), new SucursalAsignadaDto("norte", "Norte", true)],
            dto.Sucursales);
        Assert.Equal(new[] { _centro.Id, _norte.Id }.Order(), u.Sucursales.Order());
        Assert.NotEqual(sello, u.SelloSesion);
        Assert.Equal("pepe", dto.NombreUsuario);
    }

    [Fact(DisplayName = "004/RF-13: editar sin cambios (mismas sucursales en otro orden) no renueva el sello ni guarda")]
    public async Task Sin_cambios_no_renueva_el_sello()
    {
        var u = Agregar("pepe", Rol.Cajero, _centro, _norte);
        var sello = u.SelloSesion;

        var dto = await Ejecutar(u.Id, "cajero", ["norte", "centro"]);

        Assert.Equal("cajero", dto.Rol);
        Assert.Equal(sello, u.SelloSesion);
        Assert.Equal(0, _usuarios.GuardadosLlamados);
    }

    [Fact(DisplayName = "004/RF-04: una sucursal inexistente lanza ValidationException en sucursales sin cambiar nada")]
    public async Task Sucursal_inexistente_lanza_validacion()
    {
        var u = Agregar("pepe", Rol.Cajero, _centro);

        var error = await Assert.ThrowsAsync<ValidationException>(() => Ejecutar(u.Id, "cajero", ["fantasma"]));

        Assert.Equal("Sucursales", Assert.Single(error.Errors).PropertyName);
        Assert.Equal([_centro.Id], u.Sucursales);
    }

    [Fact(DisplayName = "001/RF-17: no quita el rol al último administrador activo")]
    public async Task NoQuitaRolAlUltimoAdministrador()
    {
        Agregar("viejo", Rol.AdministradorGeneral).Desactivar();
        var unico = Agregar("jefe", Rol.AdministradorGeneral);

        await Assert.ThrowsAsync<ConflictoException>(() => Ejecutar(unico.Id, "cajero", ["centro"]));

        Assert.Equal(Rol.AdministradorGeneral, unico.Rol);
        Assert.Equal(0, _usuarios.GuardadosLlamados);
    }

    [Fact(DisplayName = "004/RF-05: quitar el rol al último administrador general lanza ConflictoException sin cambiar nada")]
    public async Task Ultimo_administrador_general_conflicto()
    {
        var unico = Agregar("jefe", Rol.AdministradorGeneral);
        var sello = unico.SelloSesion;

        var error = await Assert.ThrowsAsync<ConflictoException>(() =>
            Ejecutar(unico.Id, "administrador_sucursal", ["centro"]));

        Assert.Equal("No se puede quitar el rol al último administrador activo", error.Message);
        Assert.Equal(Rol.AdministradorGeneral, unico.Rol);
        Assert.Empty(unico.Sucursales);
        Assert.Equal(sello, unico.SelloSesion);
    }

    [Fact(DisplayName = "004/RF-05: quitarse a sí mismo el rol lanza ConflictoException aunque quede otro administrador")]
    public async Task Quitarse_el_rol_a_si_mismo_conflicto()
    {
        Agregar("otra", Rol.AdministradorGeneral);
        var yo = Agregar("jefe", Rol.AdministradorGeneral);

        var error = await Assert.ThrowsAsync<ConflictoException>(() => Ejecutar(yo.Id, "cajero", ["centro"], actor: yo));

        Assert.Equal("No puedes quitarte el rol de administrador general", error.Message);
        Assert.Equal(Rol.AdministradorGeneral, yo.Rol);
        Assert.Equal(0, _usuarios.GuardadosLlamados);
    }

    [Fact(DisplayName = "001/RF-17: mantener administrador al último administrador no es conflicto")]
    public async Task MismoRolNoEsConflicto()
    {
        var unico = Agregar("jefe", Rol.AdministradorGeneral);

        var dto = await Ejecutar(unico.Id, "administrador_general", [], actor: unico);

        Assert.Equal("administrador_general", dto.Rol);
    }

    [Fact(DisplayName = "001/RF-17: quita el rol de administrador si queda otro activo")]
    public async Task QuitaRolSiQuedaOtro()
    {
        Agregar("admin", Rol.AdministradorGeneral);
        var otro = Agregar("jefe", Rol.AdministradorGeneral);

        var dto = await Ejecutar(otro.Id, "cocinero", ["centro"]);

        Assert.Equal("cocinero", dto.Rol);
    }

    [Fact(DisplayName = "001/RF-17: cambiar el rol a un administrador inactivo no cuenta como último activo")]
    public async Task AdministradorInactivoSePuedeCambiar()
    {
        Agregar("admin", Rol.AdministradorGeneral);
        var inactivo = Agregar("viejo", Rol.AdministradorGeneral);
        inactivo.Desactivar();

        var dto = await Ejecutar(inactivo.Id, "cajero", ["centro"]);

        Assert.Equal("cajero", dto.Rol);
    }

    [Fact(DisplayName = "001/RF-19: id inexistente lanza NoEncontradoException")]
    public async Task IdInexistenteLanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() => Ejecutar(Guid.NewGuid(), "cajero", ["centro"]));
    }

    [Fact(DisplayName = "004/RF-04: editar un usuario de otro restaurante lanza NoEncontradoException")]
    public async Task Usuario_de_otro_restaurante_no_encontrado()
    {
        var ajeno = Usuario.Crear(Guid.NewGuid(), "ajeno", "hash:x", Rol.Cajero, [Guid.NewGuid()], _reloj.Ahora);
        _usuarios.Agregar(ajeno);

        await Assert.ThrowsAsync<NoEncontradoException>(() => Ejecutar(ajeno.Id, "cocinero", ["centro"]));

        Assert.Equal(Rol.Cajero, ajeno.Rol);
    }

    [Fact(DisplayName = "004/RF-09: el administrador de sucursal edita a su cajero exclusivo")]
    public async Task Administrador_de_sucursal_edita_su_cajero()
    {
        var admin = Agregar("admin", Rol.AdministradorSucursal, _centro);
        var cajero = Agregar("ana", Rol.Cajero, _centro);

        var dto = await Ejecutar(cajero.Id, "cocinero", ["centro"], actor: admin);

        Assert.Equal("cocinero", dto.Rol);
        Assert.True(dto.Gestionable);
        Assert.Equal(Rol.Cocinero, cajero.Rol);
    }

    [Theory(DisplayName = "004/RF-10: el administrador de sucursal fuera de alcance recibe ProhibidoException sin cambiar nada")]
    [InlineData("cocinero", new[] { "norte" })]
    [InlineData("cocinero", new[] { "fantasma" })]
    [InlineData("cajero", new[] { "centro", "norte" })]
    [InlineData("administrador_sucursal", new[] { "centro" })]
    [InlineData("administrador_general", new string[0])]
    public async Task Administrador_de_sucursal_no_asigna_fuera_de_alcance(string rol, string[] sucursales)
    {
        var admin = Agregar("admin", Rol.AdministradorSucursal, _centro);
        var cajero = Agregar("ana", Rol.Cajero, _centro);
        var sello = cajero.SelloSesion;

        await Assert.ThrowsAsync<ProhibidoException>(() => Ejecutar(cajero.Id, rol, sucursales, actor: admin));

        Assert.Equal(Rol.Cajero, cajero.Rol);
        Assert.Equal([_centro.Id], cajero.Sucursales);
        Assert.Equal(sello, cajero.SelloSesion);
    }

    [Fact(DisplayName = "004/RF-10: el administrador de sucursal no edita personal compartido, otro administrador ni a sí mismo")]
    public async Task Administrador_de_sucursal_no_gestiona_fuera_de_alcance()
    {
        var admin = Agregar("admin", Rol.AdministradorSucursal, _centro);
        var compartido = Agregar("compartido", Rol.Cajero, _centro, _norte);
        var otroAdmin = Agregar("otro-admin", Rol.AdministradorSucursal, _centro);

        foreach (var objetivo in new[] { compartido, otroAdmin, admin })
        {
            var rol = objetivo.Rol;
            await Assert.ThrowsAsync<ProhibidoException>(() =>
                Ejecutar(objetivo.Id, "cocinero", ["centro"], actor: admin));
            Assert.Equal(rol, objetivo.Rol);
        }

        Assert.Equal(0, _usuarios.GuardadosLlamados);
    }
}
