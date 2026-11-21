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

        // La configuración inválida aborta Program antes de construir el host.
        var error = Assert.Throws<InvalidOperationException>(() => fabrica.CreateClient());

        Assert.Contains("entry point exited without ever building an IHost", error.Message);
        Assert.Contains("Configuración inválida", fabrica.SalidaDeArranque);
        Assert.Contains("JWT_SIGNING_KEY es obligatoria", fabrica.SalidaDeArranque);
    }

    [Fact(DisplayName = "001/RF-22: la API no arranca con JWT_SIGNING_KEY de menos de 32 caracteres")]
    public void La_api_no_arranca_con_clave_corta()
    {
        using var fabrica = new FabricaApi(e => e["JWT_SIGNING_KEY"] = "corta");

        var error = Assert.Throws<InvalidOperationException>(() => fabrica.CreateClient());

        Assert.Contains("entry point exited without ever building an IHost", error.Message);
        Assert.Contains("Configuración inválida", fabrica.SalidaDeArranque);
        Assert.Contains("JWT_SIGNING_KEY", fabrica.SalidaDeArranque);
        Assert.Contains("32", fabrica.SalidaDeArranque);
        Assert.DoesNotContain("corta", fabrica.SalidaDeArranque.Replace("JWT_SIGNING_KEY", string.Empty));
    }

    [Fact(DisplayName = "001/RF-08: al arrancar con la tabla vacía se crea el administrador inicial")]
    public void Al_arrancar_con_la_tabla_vacia_se_crea_el_administrador_inicial()
    {
        using var fabrica = new FabricaApi();

        using var _ = fabrica.CreateClient();

        var admin = Assert.Single(fabrica.Usuarios.Usuarios);
        Assert.Equal(Rol.Administrador, admin.Rol);
    }

    [Fact(DisplayName = "001/RF-10: la API no arranca si falta ADMIN_PASSWORD con la tabla vacía")]
    public void La_api_no_arranca_sin_ADMIN_PASSWORD_con_la_tabla_vacia()
    {
        using var fabrica = new FabricaApi(e => e.Remove("ADMIN_PASSWORD"));

        // El host se construye, pero la secuencia de arranque aborta (código 1) tras registrar el motivo.
        var error = Assert.Throws<InvalidOperationException>(() => fabrica.CreateClient());

        Assert.Contains("server has not been started", error.Message);
        Assert.Contains("No se pudo inicializar el administrador", fabrica.SalidaDeArranque);
        Assert.Contains("Falta la variable de entorno ADMIN_PASSWORD", fabrica.SalidaDeArranque);
        Assert.DoesNotContain(FabricaApi.EntornoValido()["ADMIN_PASSWORD"]!, fabrica.SalidaDeArranque);
        Assert.Empty(fabrica.Usuarios.Usuarios);
    }

    [Fact(DisplayName = "001/RF-10: la API no arranca si falta ADMIN_USERNAME con la tabla vacía")]
    public void La_api_no_arranca_sin_ADMIN_USERNAME_con_la_tabla_vacia()
    {
        using var fabrica = new FabricaApi(e => e.Remove("ADMIN_USERNAME"));

        var error = Assert.Throws<InvalidOperationException>(() => fabrica.CreateClient());

        Assert.Contains("server has not been started", error.Message);
        Assert.Contains("Falta la variable de entorno ADMIN_USERNAME", fabrica.SalidaDeArranque);
        Assert.Empty(fabrica.Usuarios.Usuarios);
    }
}
