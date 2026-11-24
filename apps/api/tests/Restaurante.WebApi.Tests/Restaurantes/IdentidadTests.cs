using System.Net;
using System.Text.Json;
using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Restaurantes;

[Collection(ColeccionApi.Nombre)]
public sealed class IdentidadTests : IDisposable
{
    private readonly FabricaApi _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    [Fact(DisplayName = "002/RF-14: GET identidad responde 200 sin token")]
    public async Task Identidad_es_anonima()
    {
        _fabrica.SembrarRestaurante("La Esquina", "la-esquina");

        var respuesta = await _fabrica.CreateClient().GetAsync("/api/r/la-esquina/identidad");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("La Esquina", json.RootElement.GetProperty("nombre").GetString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("paleta").GetProperty("principal").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("logoUrl").ValueKind);
    }

    [Fact(DisplayName = "002/RF-10: identidad de un restaurante inactivo da 404 Restaurante no disponible")]
    public async Task Identidad_de_inactivo_da_404()
    {
        _fabrica.SembrarRestaurante("Cerrado", "cerrado", activo: false);

        var respuesta = await _fabrica.CreateClient().GetAsync("/api/r/cerrado/identidad");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("Restaurante no disponible", json.RootElement.GetProperty("title").GetString());
    }
}
