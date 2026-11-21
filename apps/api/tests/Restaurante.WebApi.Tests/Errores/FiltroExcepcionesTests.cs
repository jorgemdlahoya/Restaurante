using System.Net;
using System.Text.Json;
using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Errores;

[Collection(ColeccionApi.Nombre)]
public sealed class FiltroExcepcionesTests : IDisposable
{
    private readonly FabricaApi _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    [Fact(DisplayName = "001/RF-20: excepción no controlada responde 500 con traceId y sin detalle")]
    public async Task Excepcion_no_controlada_responde_500_con_traceId_y_sin_detalle()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/prueba/explota");

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(cuerpo);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain(ControllerDePrueba.MensajeInterno, cuerpo);
        Assert.DoesNotContain("InvalidOperationException", cuerpo);
        Assert.DoesNotContain("   at ", cuerpo);
    }
}
