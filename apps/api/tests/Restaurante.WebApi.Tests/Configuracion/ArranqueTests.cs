using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Configuracion;

[Collection(ColeccionApi.Nombre)]
public sealed class ArranqueTests
{
    [Fact(DisplayName = "001/RF-22: la API no arranca sin JWT_SIGNING_KEY")]
    public void La_api_no_arranca_sin_JWT_SIGNING_KEY()
    {
        using var fabrica = new FabricaApi(e => e.Remove("JWT_SIGNING_KEY"));

        Assert.ThrowsAny<Exception>(() => fabrica.CreateClient());
    }

    [Fact(DisplayName = "001/RF-22: la API no arranca con JWT_SIGNING_KEY de menos de 32 caracteres")]
    public void La_api_no_arranca_con_clave_corta()
    {
        using var fabrica = new FabricaApi(e => e["JWT_SIGNING_KEY"] = "corta");

        Assert.ThrowsAny<Exception>(() => fabrica.CreateClient());
    }

    [Fact(DisplayName = "001/RF-22: al arrancar con la tabla vacía se crea el administrador inicial")]
    public void Al_arrancar_con_la_tabla_vacia_se_crea_el_administrador_inicial()
    {
        using var fabrica = new FabricaApi();

        using var _ = fabrica.CreateClient();

        var admin = Assert.Single(fabrica.Usuarios.Usuarios);
        Assert.Equal(Rol.Administrador, admin.Rol);
    }

    [Fact(DisplayName = "001/RF-22: la API no arranca si falta ADMIN_PASSWORD con la tabla vacía")]
    public void La_api_no_arranca_sin_ADMIN_PASSWORD_con_la_tabla_vacia()
    {
        using var fabrica = new FabricaApi(e => e.Remove("ADMIN_PASSWORD"));

        Assert.ThrowsAny<Exception>(() => fabrica.CreateClient());
        Assert.Empty(fabrica.Usuarios.Usuarios);
    }
}
