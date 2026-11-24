using Restaurante.Domain.Usuarios;

namespace Restaurante.Domain.Tests.Usuarios;

public class UsuarioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid RestauranteId = Guid.NewGuid();

    private static Usuario NuevoUsuario(Rol rol = Rol.Cajero) =>
        Usuario.Crear(RestauranteId, "Ana.Mesera", "hash", rol, [], Ahora);

    [Fact(DisplayName = "001/RF-11: un usuario nuevo está activo y normaliza el nombre")]
    public void Crear_UsuarioNuevo_EstaActivoYNormalizaElNombre()
    {
        var usuario = NuevoUsuario();

        Assert.True(usuario.Activo);
        Assert.NotEqual(Guid.Empty, usuario.Id);
        Assert.Equal("Ana.Mesera", usuario.NombreUsuario);
        Assert.Equal("ana.mesera", usuario.NombreUsuarioNormalizado);
        Assert.Equal("hash", usuario.HashContrasena);
        Assert.Equal(Rol.Cajero, usuario.Rol);
        Assert.Equal(Ahora, usuario.CreadoEn);
        Assert.Equal(RestauranteId, usuario.RestauranteId);
    }

    [Fact(DisplayName = "002/RF-13: sólo el administrador de plataforma carece de restaurante")]
    public void Crear_SoloElAdministradorDePlataformaCareceDeRestaurante()
    {
        Assert.Throws<ArgumentException>(() => Usuario.Crear(null, "ana", "hash", Rol.Cajero, [], Ahora));
        Assert.Throws<ArgumentException>(() =>
            Usuario.Crear(Guid.NewGuid(), "root", "hash", Rol.AdministradorPlataforma, [], Ahora));

        var plataforma = Usuario.Crear(null, "root", "hash", Rol.AdministradorPlataforma, [], Ahora);
        Assert.Null(plataforma.RestauranteId);
        foreach (var rol in new[] { Rol.AdministradorGeneral, Rol.AdministradorSucursal, Rol.Cajero, Rol.Cocinero })
        {
            Assert.Throws<ArgumentException>(() => Usuario.Crear(null, "ana", "hash", rol, [], Ahora));
            Assert.Equal(RestauranteId, Usuario.Crear(RestauranteId, "ana", "hash", rol, [], Ahora).RestauranteId);
        }
    }

    [Fact(DisplayName = "001/RF-15: desactivar y reactivar")]
    public void DesactivarYReactivar_CambiaElEstado()
    {
        var usuario = NuevoUsuario();

        usuario.Desactivar();
        Assert.False(usuario.Activo);

        usuario.Reactivar();
        Assert.True(usuario.Activo);
    }

    [Fact(DisplayName = "001/RF-16: cambiar rol")]
    public void CambiarRol_AsignaElNuevoRol()
    {
        var usuario = NuevoUsuario();

        usuario.CambiarRol(Rol.Cajero);

        Assert.Equal(Rol.Cajero, usuario.Rol);
    }
}
