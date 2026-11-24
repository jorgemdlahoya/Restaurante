using System.Net;
using System.Text.Json;
using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Plataforma;

[Collection(ColeccionApi.Nombre)]
public sealed class SwaggerTests
{
    private const string RutaDocumento = "/swagger/v1/swagger.json";

    [Fact(DisplayName = "001/RF-25: Swagger disponible en Development con esquema Bearer")]
    public async Task Swagger_disponible_en_development_con_esquema_bearer()
    {
        using var fabrica = new FabricaApi(entornoHospedaje: "Development");
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync(RutaDocumento);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;
        var esquema = raiz.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", esquema.GetProperty("type").GetString());
        Assert.Equal("bearer", esquema.GetProperty("scheme").GetString());
        Assert.Equal("JWT", esquema.GetProperty("bearerFormat").GetString());
        Assert.True(raiz.GetProperty("paths").TryGetProperty("/api/r/{slug}/auth/login", out _));
        Assert.True(raiz.GetProperty("paths").TryGetProperty("/api/r/{slug}/usuarios", out _));
    }

    private static async Task<JsonElement> LeerOperacionAsync(string ruta, string metodo)
    {
        using var fabrica = new FabricaApi(entornoHospedaje: "Development");
        using var cliente = fabrica.CreateClient();
        var texto = await cliente.GetStringAsync(RutaDocumento);
        using var documento = JsonDocument.Parse(texto);
        return documento.RootElement.GetProperty("paths").GetProperty(ruta).GetProperty(metodo).Clone();
    }

    private static string[] Codigos(JsonElement operacion) =>
        operacion.GetProperty("responses").EnumerateObject().Select(p => p.Name).Order().ToArray();

    [Fact(DisplayName = "001/RF-25: login no exige token en Swagger y el listado de usuarios sí")]
    public async Task Login_no_exige_token_y_usuarios_si()
    {
        var login = await LeerOperacionAsync("/api/r/{slug}/auth/login", "post");
        var loginPlataforma = await LeerOperacionAsync("/api/plataforma/auth/login", "post");
        var listado = await LeerOperacionAsync("/api/r/{slug}/usuarios", "get");

        Assert.False(login.TryGetProperty("security", out _), "login no debe declarar requisito de seguridad");
        Assert.False(loginPlataforma.TryGetProperty("security", out _), "login de plataforma tampoco");
        var requisitos = listado.GetProperty("security").EnumerateArray().ToArray();
        var requisito = Assert.Single(requisitos);
        Assert.True(requisito.TryGetProperty("Bearer", out _));
    }

    [Fact(DisplayName = "001/RF-25: Swagger documenta las respuestas de login y de usuarios")]
    public async Task Swagger_documenta_las_respuestas()
    {
        Assert.Equal(["200", "400", "401", "404"], Codigos(await LeerOperacionAsync("/api/r/{slug}/auth/login", "post")));
        Assert.Equal(["200", "400", "401"], Codigos(await LeerOperacionAsync("/api/plataforma/auth/login", "post")));
        // 404 en todas: restaurante no disponible (o token de otro restaurante) en /api/r/{slug}.
        Assert.Equal(["200", "401", "403", "404"], Codigos(await LeerOperacionAsync("/api/r/{slug}/usuarios", "get")));
        Assert.Equal(["201", "400", "401", "403", "404", "409"],
            Codigos(await LeerOperacionAsync("/api/r/{slug}/usuarios", "post")));
        Assert.Equal(["200", "400", "401", "403", "404", "409"],
            Codigos(await LeerOperacionAsync("/api/r/{slug}/usuarios/{id}/estado", "put")));
        Assert.Equal(["200", "400", "401", "403", "404", "409"],
            Codigos(await LeerOperacionAsync("/api/r/{slug}/usuarios/{id}/rol", "put")));
    }

    [Fact(DisplayName = "001/RF-25: Swagger no disponible en Production")]
    public async Task Swagger_no_disponible_en_production()
    {
        using var fabrica = new FabricaApi(entornoHospedaje: "Production");
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync(RutaDocumento);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
