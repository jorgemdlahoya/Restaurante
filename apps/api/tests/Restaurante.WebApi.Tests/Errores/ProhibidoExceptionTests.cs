using System.Net;
using System.Text.Json;
using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Errores;

[Collection(ColeccionApi.Nombre)]
public sealed class ProhibidoExceptionTests : IDisposable
{
    private readonly FabricaApi _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    [Fact(DisplayName = "004/RF-10: ProhibidoException se traduce en 403 Acceso denegado")]
    public async Task ProhibidoException_responde_403_acceso_denegado_con_su_mensaje()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/prueba/prohibido");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(403, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Acceso denegado", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(ControllerDePrueba.MensajeProhibido, json.RootElement.GetProperty("detail").GetString());
    }
}
