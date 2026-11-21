using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Plataforma;

[Collection(ColeccionApi.Nombre)]
public sealed class CorsTests : IDisposable
{
    private const string OrigenPermitido = "http://localhost:5173";

    private readonly FabricaApi _fabrica = new(e => e["CORS_ALLOWED_ORIGINS"] = $"{OrigenPermitido},https://app.ejemplo.com");

    public void Dispose() => _fabrica.Dispose();

    private async Task<HttpResponseMessage> EnviarAsync(string origen)
    {
        using var cliente = _fabrica.CreateClient();
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/health");
        peticion.Headers.Add("Origin", origen);
        return await cliente.SendAsync(peticion);
    }

    [Fact(DisplayName = "001/RF-24: origen permitido recibe Access-Control-Allow-Origin")]
    public async Task Origen_permitido_recibe_la_cabecera()
    {
        foreach (var origen in new[] { OrigenPermitido, "https://app.ejemplo.com" })
        {
            var respuesta = await EnviarAsync(origen);

            Assert.True(respuesta.Headers.TryGetValues("Access-Control-Allow-Origin", out var valores));
            Assert.Equal(origen, Assert.Single(valores));
        }
    }

    [Fact(DisplayName = "001/RF-24: origen no listado no la recibe")]
    public async Task Origen_no_listado_no_recibe_la_cabecera()
    {
        var respuesta = await EnviarAsync("https://malicioso.example");

        Assert.False(respuesta.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
