using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Seguridad;

[Collection(ColeccionApi.Nombre)]
public sealed class AutorizacionTests : IDisposable
{
    private readonly FabricaApi _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    private async Task<HttpResponseMessage> GetAsync(string ruta, string? token = null)
    {
        var cliente = _fabrica.CreateClient();
        if (token is not null)
        {
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await cliente.GetAsync(ruta);
    }

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage respuesta, HttpStatusCode estado)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal((int)estado, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("title").GetString()));
    }

    [Fact(DisplayName = "001/RF-05: sin token responde 401 ProblemDetails")]
    public async Task Sin_token_responde_401_ProblemDetails()
    {
        var respuesta = await GetAsync("/prueba/caja");

        await AssertProblemDetailsAsync(respuesta, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "001/RF-05: el 401 incluye WWW-Authenticate: Bearer (RFC 6750)")]
    public async Task El_401_incluye_WWW_Authenticate_Bearer()
    {
        var respuesta = await GetAsync("/prueba/caja");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        var desafio = Assert.Single(respuesta.Headers.WwwAuthenticate);
        Assert.Equal("Bearer", desafio.Scheme);
    }

    [Fact(DisplayName = "001/RF-06: el 403 no incluye WWW-Authenticate")]
    public async Task El_403_no_incluye_WWW_Authenticate()
    {
        var respuesta = await GetAsync("/prueba/caja", _fabrica.EmitirToken(Rol.Cocinero));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Empty(respuesta.Headers.WwwAuthenticate);
    }

    [Fact(DisplayName = "001/RF-05: token con otra clave responde 401")]
    public async Task Token_con_otra_clave_responde_401()
    {
        var token = _fabrica.EmitirToken(Rol.Cajero, clave: FabricaApi.ClaveAjena);

        var respuesta = await GetAsync("/prueba/caja", token);

        await AssertProblemDetailsAsync(respuesta, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "001/RF-05: token expirado responde 401")]
    public async Task Token_expirado_responde_401()
    {
        var token = _fabrica.EmitirToken(Rol.Cajero, expiraEn: TimeSpan.FromHours(-1));

        var respuesta = await GetAsync("/prueba/caja", token);

        await AssertProblemDetailsAsync(respuesta, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "001/RF-05: token expirado hace 30 s responde 401 (sin tolerancia de reloj)")]
    public async Task Token_expirado_hace_30_segundos_responde_401()
    {
        var token = _fabrica.EmitirToken(Rol.Cajero, expiraEn: TimeSpan.FromSeconds(-30));

        var respuesta = await GetAsync("/prueba/caja", token);

        await AssertProblemDetailsAsync(respuesta, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "001/RF-05: token con la clave correcta y otro emisor responde 401")]
    public async Task Token_con_otro_emisor_responde_401()
    {
        var token = _fabrica.EmitirToken(Rol.Cajero, emisor: "otro-emisor");

        var respuesta = await GetAsync("/prueba/caja", token);

        await AssertProblemDetailsAsync(respuesta, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "001/RF-05: token con la clave correcta y otra audiencia responde 401")]
    public async Task Token_con_otra_audiencia_responde_401()
    {
        var token = _fabrica.EmitirToken(Rol.Cajero, audiencia: "otra-audiencia");

        var respuesta = await GetAsync("/prueba/caja", token);

        await AssertProblemDetailsAsync(respuesta, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "001/RF-05: Bearer con basura responde 401 ProblemDetails")]
    public async Task Bearer_con_basura_responde_401_ProblemDetails()
    {
        var respuesta = await GetAsync("/prueba/caja", "abc");

        await AssertProblemDetailsAsync(respuesta, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "001/RF-06: rol no autorizado responde 403 ProblemDetails")]
    public async Task Rol_no_autorizado_responde_403_ProblemDetails()
    {
        var token = _fabrica.EmitirToken(Rol.Cocinero);

        var respuesta = await GetAsync("/prueba/caja", token);

        await AssertProblemDetailsAsync(respuesta, HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "001/RF-06: rol autorizado accede al endpoint")]
    public async Task Rol_autorizado_accede_al_endpoint()
    {
        var token = _fabrica.EmitirToken(Rol.Cajero);

        var respuesta = await GetAsync("/prueba/caja", token);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact(DisplayName = "001/RF-07: el endpoint de prueba anónimo no exige token")]
    public async Task El_endpoint_anonimo_no_exige_token()
    {
        var respuesta = await GetAsync("/prueba/anonimo");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
