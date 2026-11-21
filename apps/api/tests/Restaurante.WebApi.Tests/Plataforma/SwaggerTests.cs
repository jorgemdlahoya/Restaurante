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
        Assert.True(raiz.GetProperty("paths").TryGetProperty("/api/auth/login", out _));
        Assert.True(raiz.GetProperty("paths").TryGetProperty("/api/usuarios", out _));
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
