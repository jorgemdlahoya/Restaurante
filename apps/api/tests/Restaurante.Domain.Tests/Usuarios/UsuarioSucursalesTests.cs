using Restaurante.Domain.Usuarios;

namespace Restaurante.Domain.Tests.Usuarios;

public class UsuarioSucursalesTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid RestauranteId = Guid.NewGuid();

    private static Usuario Nuevo(Rol rol, params Guid[] sucursales) =>
        Usuario.Crear(RestauranteId, "ana", "hash", rol, sucursales, Ahora);

    [Theory(DisplayName = "004/RF-02: la cardinalidad de sucursales depende del rol")]
    [InlineData(Rol.AdministradorGeneral, 0, true)]
    [InlineData(Rol.AdministradorSucursal, 1, true)]
    [InlineData(Rol.Cajero, 1, true)]
    [InlineData(Rol.Cocinero, 3, true)]
    [InlineData(Rol.AdministradorGeneral, 1, false)]
    [InlineData(Rol.AdministradorSucursal, 0, false)]
    [InlineData(Rol.AdministradorSucursal, 2, false)]
    [InlineData(Rol.Cajero, 0, false)]
    public void ValidarSucursales_SegunElRol(Rol rol, int cantidad, bool valido)
    {
        if (valido)
        {
            Usuario.ValidarSucursales(rol, cantidad);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => Usuario.ValidarSucursales(rol, cantidad));
        }
    }

    [Theory(DisplayName = "004/RF-02: Crear aplica la cardinalidad de sucursales del rol")]
    [InlineData(Rol.AdministradorGeneral, 1)]
    [InlineData(Rol.AdministradorSucursal, 0)]
    [InlineData(Rol.AdministradorSucursal, 2)]
    [InlineData(Rol.Cajero, 0)]
    [InlineData(Rol.Cocinero, 0)]
    public void Crear_AplicaLaCardinalidad(Rol rol, int cantidad)
    {
        var sucursales = Enumerable.Range(0, cantidad).Select(_ => Guid.NewGuid()).ToArray();

        Assert.Throws<ArgumentException>(() => Nuevo(rol, sucursales));
    }

    [Fact(DisplayName = "004/RF-02: Crear rechaza sucursales duplicadas")]
    public void Crear_RechazaSucursalesDuplicadas()
    {
        var a = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => Nuevo(Rol.Cajero, a, a));
    }

    [Fact(DisplayName = "004/RF-13: un usuario nuevo tiene sello y sus sucursales")]
    public void Crear_TieneSelloYSucursales()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var usuario = Nuevo(Rol.Cajero, a, b);

        Assert.NotEqual(Guid.Empty, usuario.SelloSesion);
        Assert.Equal(new[] { a, b }.Order(), usuario.Sucursales.Order());
    }

    [Fact(DisplayName = "004/RF-13: cambiar rol o sucursales renueva el sello")]
    public void AsignarRolYSucursales_CuandoCambia_RenuevaElSello()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var usuario = Nuevo(Rol.Cajero, a);

        var sello1 = usuario.SelloSesion;
        Assert.True(usuario.AsignarRolYSucursales(Rol.Cajero, [a, b]));
        var sello2 = usuario.SelloSesion;
        Assert.NotEqual(sello1, sello2);
        Assert.Equal(2, usuario.Sucursales.Count);

        Assert.True(usuario.AsignarRolYSucursales(Rol.Cocinero, [a, b]));
        Assert.NotEqual(sello2, usuario.SelloSesion);
        Assert.Equal(Rol.Cocinero, usuario.Rol);
    }

    [Fact(DisplayName = "004/RF-13: reasignar lo mismo en otro orden no renueva el sello")]
    public void AsignarRolYSucursales_MismoConjuntoEnOtroOrden_NoRenueva()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var usuario = Nuevo(Rol.Cajero, a, b);
        var sello = usuario.SelloSesion;

        Assert.False(usuario.AsignarRolYSucursales(Rol.Cajero, [b, a]));

        Assert.Equal(sello, usuario.SelloSesion);
    }

    [Fact(DisplayName = "004/RF-13: desactivar y cambiar contraseña renuevan el sello; reactivar no")]
    public void DesactivarYCambiarContrasena_RenuevanElSello_ReactivarNo()
    {
        var usuario = Nuevo(Rol.Cajero, Guid.NewGuid());

        var s0 = usuario.SelloSesion;
        usuario.Desactivar();
        var s1 = usuario.SelloSesion;
        Assert.NotEqual(s0, s1);

        usuario.Reactivar();
        Assert.Equal(s1, usuario.SelloSesion);

        usuario.CambiarContrasena("otro-hash");
        Assert.NotEqual(s1, usuario.SelloSesion);
        Assert.Equal("otro-hash", usuario.HashContrasena);
    }
}
