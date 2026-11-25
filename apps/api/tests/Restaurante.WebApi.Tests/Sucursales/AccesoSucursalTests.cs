using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Sucursales;

/// <summary>Acceso sólo a sucursales asignadas y activas (004/RF-15, RF-25).</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class AccesoSucursalTests : IDisposable
{
    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _restaurante;
    private readonly Sucursal _centro;
    private readonly Sucursal _norte;

    public AccesoSucursalTests()
    {
        _restaurante = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        _centro = _fabrica.SembrarSucursal(_restaurante.Id, "centro", "Centro", "C");
        _norte = _fabrica.SembrarSucursal(_restaurante.Id, "norte", "Norte", "N");
    }

    public void Dispose() => _fabrica.Dispose();

    private HttpClient ClienteDe(Rol rol, params Sucursal[] sucursales)
    {
        var usuario = Usuario.Crear(
            _restaurante.Id, $"u-{Guid.NewGuid():N}", "sin-hash", rol, sucursales.Select(s => s.Id).ToList(),
            DateTimeOffset.UtcNow);
        _fabrica.Usuarios.Agregar(usuario);
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", _fabrica.EmitirToken(rol, usuarioId: usuario.Id, restauranteId: _restaurante.Id));
        return cliente;
    }

    private static string Ruta(string sucursal) => $"/api/r/la-esquina/s/{sucursal}/eco-acceso";

    private static async Task AssertNoDisponibleAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("Sucursal no disponible", json.RootElement.GetProperty("title").GetString());
    }

    [Fact(DisplayName = "004/RF-15: un cajero accede a su sucursal")]
    public async Task Cajero_accede_a_su_sucursal()
    {
        var respuesta = await ClienteDe(Rol.Cajero, _centro).GetAsync(Ruta("centro"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact(DisplayName = "004/RF-15: un cajero recibe 404 en una sucursal no asignada")]
    public async Task Cajero_recibe_404_en_sucursal_no_asignada()
    {
        var respuesta = await ClienteDe(Rol.Cajero, _centro).GetAsync(Ruta("norte"));

        await AssertNoDisponibleAsync(respuesta);
    }

    [Fact(DisplayName = "004/RF-25: un cajero recibe 404 en su sucursal desactivada")]
    public async Task Cajero_recibe_404_en_sucursal_desactivada()
    {
        _centro.Desactivar();

        var respuesta = await ClienteDe(Rol.Cajero, _centro, _norte).GetAsync(Ruta("centro"));

        await AssertNoDisponibleAsync(respuesta);
    }

    [Fact(DisplayName = "004/RF-15: el administrador general accede a cualquier sucursal")]
    public async Task General_accede_a_cualquier_sucursal()
    {
        var cliente = ClienteDe(Rol.AdministradorGeneral);

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(Ruta("centro"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(Ruta("norte"))).StatusCode);
    }

    [Fact(DisplayName = "004/RF-15: el estado público no cambia")]
    public async Task Estado_publico_no_cambia()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/api/r/la-esquina/s/centro/estado");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
