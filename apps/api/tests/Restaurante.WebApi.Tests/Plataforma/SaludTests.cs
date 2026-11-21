using System.Net;
using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Plataforma;

[Collection(ColeccionApi.Nombre)]
public sealed class SaludTests
{
    [Fact(DisplayName = "001/RF-23: /health 200 con base accesible")]
    public async Task Health_200_con_base_accesible()
    {
        using var fabrica = new FabricaApi { BaseAccesible = true };
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact(DisplayName = "001/RF-23: /health 503 con base inaccesible")]
    public async Task Health_503_con_base_inaccesible()
    {
        using var fabrica = new FabricaApi { BaseAccesible = false };
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
    }

    [Fact(DisplayName = "001/RF-07: /health no exige token")]
    public async Task Health_no_exige_token()
    {
        using var fabrica = new FabricaApi();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/health");

        Assert.NotEqual(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
