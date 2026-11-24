using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Sucursales;

[Collection(ColeccionApi.Nombre)]
public sealed class ContextoSucursalTests : IDisposable
{
    private const string TituloNoDisponible = "Sucursal no disponible";
    private const string DetalleNoDisponible = "La dirección no corresponde a una sucursal de este restaurante.";

    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _a;
    private readonly RestauranteEntidad _b;

    public ContextoSucursalTests()
    {
        _a = _fabrica.SembrarRestaurante("Resto A", "resto-a");
        _b = _fabrica.SembrarRestaurante("Resto B", "resto-b");
    }

    public void Dispose() => _fabrica.Dispose();

    private Sucursal Sembrar(Guid restauranteId, string slug, string whatsApp, string prefijo)
    {
        var sucursal = Sucursal.Crear(
            restauranteId,
            slug,
            new DatosSucursal("Sucursal " + slug, "Calle 1", "+525500000000", whatsApp, null, prefijo, ZonasHorarias.PorDefecto),
            DateTimeOffset.UtcNow);
        _fabrica.Sucursales.Sucursales.Add(sucursal);
        return sucursal;
    }

    private static async Task<Guid?> SucursalDelEcoAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var valor = json.RootElement.GetProperty("sucursalId");
        return valor.ValueKind == JsonValueKind.Null ? null : valor.GetGuid();
    }

    private static async Task AssertNoDisponibleAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(TituloNoDisponible, json.RootElement.GetProperty("title").GetString());
        Assert.Equal(DetalleNoDisponible, json.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "003/RF-15: la ruta de una sucursal fija su contexto")]
    public async Task Ruta_de_sucursal_fija_su_contexto()
    {
        var centro = Sembrar(_a.Id, "centro", "+525511111111", "C");
        Sembrar(_a.Id, "norte", "+525522222222", "N");
        var cliente = _fabrica.CreateClient();

        var minusculas = await SucursalDelEcoAsync(await cliente.GetAsync("/api/r/resto-a/s/centro/eco-contexto"));
        var mayusculas = await SucursalDelEcoAsync(await cliente.GetAsync("/api/r/resto-a/s/CENTRO/eco-contexto"));

        Assert.Equal(centro.Id, minusculas);
        Assert.Equal(centro.Id, mayusculas);
    }

    [Fact(DisplayName = "003/RF-15: una sucursal inactiva también fija su contexto")]
    public async Task Sucursal_inactiva_fija_su_contexto()
    {
        var centro = Sembrar(_a.Id, "centro", "+525511111111", "C");
        centro.Desactivar();

        var id = await SucursalDelEcoAsync(
            await _fabrica.CreateClient().GetAsync("/api/r/resto-a/s/centro/eco-contexto"));

        Assert.Equal(centro.Id, id);
    }

    [Fact(DisplayName = "003/RF-15: una sucursal de otro restaurante en la ruta da 404")]
    public async Task Sucursal_de_otro_restaurante_da_404()
    {
        Sembrar(_a.Id, "centro", "+525511111111", "C");
        Sembrar(_b.Id, "solo-de-b", "+525533333333", "B");

        var respuesta = await _fabrica.CreateClient().GetAsync("/api/r/resto-a/s/solo-de-b/eco-contexto");

        await AssertNoDisponibleAsync(respuesta);
    }

    [Fact(DisplayName = "003/RF-14: un slug de sucursal inexistente da 404 Sucursal no disponible")]
    public async Task Slug_inexistente_da_404()
    {
        Sembrar(_a.Id, "centro", "+525511111111", "C");

        var respuesta = await _fabrica.CreateClient().GetAsync("/api/r/resto-a/s/no-existe/eco-contexto");

        await AssertNoDisponibleAsync(respuesta);
    }

    [Fact(DisplayName = "003/RF-15: fuera de /s/ no hay contexto de sucursal")]
    public async Task Fuera_de_s_no_hay_contexto()
    {
        Sembrar(_a.Id, "centro", "+525511111111", "C");
        var admin = _fabrica.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FabricaApi.EmitirToken(Rol.AdministradorGeneral, restauranteId: _a.Id));

        var listado = await admin.GetAsync("/api/r/resto-a/sucursales");
        var eco = await SucursalDelEcoAsync(await _fabrica.CreateClient().GetAsync("/api/r/resto-a/eco-contexto"));

        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
        Assert.Null(eco);
    }
}
