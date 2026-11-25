using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Usuarios;
using Restaurante.Application.Features.Usuarios.CambiarContrasenaUsuario;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Usuarios;

public class CambiarContrasenaUsuarioTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private static readonly ContextoRestauranteFalso Contexto = new(RestauranteId);
    private readonly UsuarioRepositoryEnMemoria _repositorio = new(Contexto);
    private readonly RelojFijo _reloj = new();
    private Guid _actual = Guid.NewGuid();

    private Usuario Agregar(string nombre, Rol rol)
    {
        var u = Usuario.Crear(RestauranteId, nombre, "hash:vieja", rol, SucursalesDePrueba.Para(rol), _reloj.Ahora);
        _repositorio.Agregar(u);
        return u;
    }

    private Task Ejecutar(Guid id, string contrasena) =>
        new CambiarContrasenaUsuarioHandler(
                _repositorio,
                new ResolutorActor(new UsuarioActualFalso(_actual), new SucursalRepositoryEnMemoria(Contexto)),
                new HasherFalso())
            .Handle(new CambiarContrasenaUsuarioCommand(id, contrasena), CancellationToken.None);

    [Fact(DisplayName = "004/RF-06: guarda sólo el hash de la contraseña nueva y renueva el sello")]
    public async Task GuardaElHashYRenuevaElSello()
    {
        _actual = Agregar("jefa", Rol.AdministradorGeneral).Id;
        var cajero = Agregar("pepe", Rol.Cajero);
        var sello = cajero.SelloSesion;

        await Ejecutar(cajero.Id, "Clave-nueva-456");

        Assert.Equal("hash:Clave-nueva-456", cajero.HashContrasena);
        Assert.NotEqual(sello, cajero.SelloSesion);
        Assert.Equal(1, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "004/RF-06: el administrador general puede cambiar la suya")]
    public async Task ElGeneralCambiaLaSuya()
    {
        var admin = Agregar("jefa", Rol.AdministradorGeneral);
        _actual = admin.Id;

        await Ejecutar(admin.Id, "Clave-nueva-456");

        Assert.Equal("hash:Clave-nueva-456", admin.HashContrasena);
    }

    [Fact(DisplayName = "004/RF-06: un usuario inexistente responde 404 y no guarda")]
    public async Task UsuarioInexistente()
    {
        _actual = Agregar("jefa", Rol.AdministradorGeneral).Id;

        await Assert.ThrowsAsync<NoEncontradoException>(() => Ejecutar(Guid.NewGuid(), "Clave-nueva-456"));

        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Theory(DisplayName = "004/RF-06: la contraseña vacía o de menos de 8 caracteres no es válida")]
    [InlineData("")]
    [InlineData("1234567")]
    public void ContrasenaInvalida(string contrasena) =>
        Assert.False(new CambiarContrasenaUsuarioValidator()
            .Validate(new CambiarContrasenaUsuarioCommand(Guid.NewGuid(), contrasena)).IsValid);

    [Fact(DisplayName = "004/RF-06: una contraseña de 8 caracteres es válida")]
    public void OchoCaracteresEsValida() =>
        Assert.True(new CambiarContrasenaUsuarioValidator()
            .Validate(new CambiarContrasenaUsuarioCommand(Guid.NewGuid(), "12345678")).IsValid);
}
