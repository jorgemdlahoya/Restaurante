using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using static Restaurante.WebApi.Tests.Infraestructura.AfirmacionesHttp;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Sucursales;

[Collection(ColeccionApi.Nombre)]
public sealed class CierresEndpointsTests : IDisposable
{
    // 2026-11-24 18:00Z = 12:00 en Ciudad de México (UTC-6) y 10:00 en Tijuana (UTC-8, invierno).
    private static readonly DateTimeOffset Ahora = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    private readonly FabricaApi _fabrica;
    private readonly RestauranteEntidad _a;
    private readonly RestauranteEntidad _b;
    private readonly Sucursal _centro;
    private readonly Sucursal _norte;
    private readonly Sucursal _tijuana;

    public CierresEndpointsTests()
    {
        var reloj = new RelojFijo(Ahora);
        _fabrica = new FabricaApi
        {
            AjustarServicios = servicios =>
            {
                servicios.RemoveAll<IReloj>();
                servicios.AddSingleton<IReloj>(reloj);
            },
        };
        _a = _fabrica.SembrarRestaurante("Resto A", "resto-a");
        _b = _fabrica.SembrarRestaurante("Resto B", "resto-b");
        _centro = Sembrar(_a.Id, "centro", "+525511111111", "C", ZonasHorarias.PorDefecto);
        _norte = Sembrar(_a.Id, "norte", "+525522222222", "N", ZonasHorarias.PorDefecto);
        _tijuana = Sembrar(_a.Id, "tijuana", "+525544444444", "T", "America/Tijuana");
        Sembrar(_b.Id, "solo-de-b", "+525533333333", "B", ZonasHorarias.PorDefecto);
    }

    public void Dispose() => _fabrica.Dispose();

    private Sucursal Sembrar(Guid restauranteId, string slug, string whatsApp, string prefijo, string zona)
    {
        var sucursal = Sucursal.Crear(
            restauranteId,
            slug,
            new DatosSucursal("Sucursal " + slug, "Calle 1", "+525500000000", whatsApp, null, prefijo, zona),
            Ahora);
        _fabrica.Sucursales.Sucursales.Add(sucursal);
        _fabrica.Sucursales.Horarios.AddRange(HorarioDia.SemanaCerrada(restauranteId, sucursal.Id));
        return sucursal;
    }

    private HttpClient ClienteComo(Rol rol)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FabricaApi.EmitirToken(rol, restauranteId: _a.Id));
        return cliente;
    }

    private HttpClient AdminDeA() => ClienteComo(Rol.AdministradorGeneral);

    private static async Task<JsonElement> LeerAsync(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        Assert.Equal(esperado, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    private static async Task AssertNoEncontradoAsync(HttpResponseMessage respuesta, string titulo, string detalle)
    {
        var json = await LeerAsync(respuesta, HttpStatusCode.NotFound);
        Assert.Equal(titulo, json.GetProperty("title").GetString());
        Assert.Equal(detalle, json.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "003/RF-10: crear un cierre sin inicio empieza ahora y se lista")]
    public async Task Crear_sin_inicio_y_listar()
    {
        var cliente = AdminDeA();

        var post = await cliente.PostAsJsonAsync(
            "/api/r/resto-a/s/centro/cierres", new { fin = "2026-11-25T10:00", motivo = "Remodelación" });

        var creado = await LeerAsync(post, HttpStatusCode.Created);
        Assert.Equal("2026-11-24T12:00", creado.GetProperty("inicio").GetString());
        Assert.Equal("2026-11-25T10:00", creado.GetProperty("fin").GetString());
        Assert.Equal("Remodelación", creado.GetProperty("motivo").GetString());
        var id = creado.GetProperty("id").GetGuid();
        Assert.Contains($"/api/r/resto-a/s/centro/cierres/{id}", post.Headers.Location?.ToString());

        var lista = await LeerAsync(await cliente.GetAsync("/api/r/resto-a/s/centro/cierres"), HttpStatusCode.OK);
        var unico = Assert.Single(lista.EnumerateArray());
        Assert.Equal(creado.ToString(), unico.ToString());
        var guardado = Assert.Single(_fabrica.Cierres.Cierres);
        Assert.Equal(Ahora, guardado.Inicio);
        Assert.Equal(_centro.Id, guardado.SucursalId);
        Assert.Equal(_a.Id, guardado.RestauranteId);
    }

    [Theory(DisplayName = "003/RF-10: fin anterior al inicio, fin pasado o fecha mal escrita dan 400")]
    [InlineData("2026-12-01T10:00", "2026-12-01T09:00", null, "fin", "El fin debe ser posterior al inicio")]
    [InlineData("2026-12-01T10:00", "2026-12-01T10:00", null, "fin", "El fin debe ser posterior al inicio")]
    [InlineData("2026-11-01T10:00", "2026-11-10T10:00", null, "fin", "El fin no puede estar en el pasado")]
    [InlineData("mañana", "2026-12-01T10:00", null, "inicio", "La fecha debe tener el formato yyyy-MM-ddTHH:mm")]
    [InlineData("2026-12-01T10:00", "2026-12-01 10:00", null, "fin", "La fecha debe tener el formato yyyy-MM-ddTHH:mm")]
    [InlineData("2026-12-01T10:00", "2026-13-01T10:00", null, "fin", "La fecha debe tener el formato yyyy-MM-ddTHH:mm")]
    [InlineData("2026-12-01T10:00", null, null, "fin", "La fecha debe tener el formato yyyy-MM-ddTHH:mm")]
    public async Task Fechas_invalidas(string? inicio, string? fin, string? motivo, string clave, string mensaje)
    {
        var respuesta = await AdminDeA().PostAsJsonAsync("/api/r/resto-a/s/centro/cierres", new { inicio, fin, motivo });

        await AssertValidacionAsync(respuesta, clave, mensaje);
        Assert.Empty(_fabrica.Cierres.Cierres);
    }

    [Fact(DisplayName = "003/RF-10: un motivo de más de 120 caracteres da 400 y uno de 120 se acepta")]
    public async Task Motivo_demasiado_largo()
    {
        var cliente = AdminDeA();

        var largo = await cliente.PostAsJsonAsync(
            "/api/r/resto-a/s/centro/cierres", new { fin = "2026-12-01T10:00", motivo = new string('m', 121) });
        var justo = await cliente.PostAsJsonAsync(
            "/api/r/resto-a/s/centro/cierres", new { fin = "2026-12-01T10:00", motivo = new string('m', 120) });

        await AssertValidacionAsync(largo, "motivo", "El motivo no puede superar 120 caracteres");
        Assert.Equal(HttpStatusCode.Created, justo.StatusCode);
    }

    [Fact(DisplayName = "003/RF-10: las fechas se interpretan en la zona de la sucursal")]
    public async Task Fechas_en_la_zona_de_la_sucursal()
    {
        var cliente = AdminDeA();

        var post = await cliente.PostAsJsonAsync(
            "/api/r/resto-a/s/tijuana/cierres", new { inicio = "2026-12-24T09:00", fin = "2026-12-25T09:00" });

        var creado = await LeerAsync(post, HttpStatusCode.Created);
        Assert.Equal("2026-12-24T09:00", creado.GetProperty("inicio").GetString());
        var guardado = Assert.Single(_fabrica.Cierres.Cierres);
        Assert.Equal(new DateTimeOffset(2026, 12, 24, 17, 0, 0, TimeSpan.Zero), guardado.Inicio);
        Assert.Equal(new DateTimeOffset(2026, 12, 25, 17, 0, 0, TimeSpan.Zero), guardado.Fin);
        var lista = await LeerAsync(await cliente.GetAsync("/api/r/resto-a/s/tijuana/cierres"), HttpStatusCode.OK);
        Assert.Equal("2026-12-24T09:00", Assert.Single(lista.EnumerateArray()).GetProperty("inicio").GetString());
    }

    [Fact(DisplayName = "003/RF-11: cancelar un cierre lo saca de la lista de vigentes")]
    public async Task Cancelar_lo_saca_de_la_lista()
    {
        var cliente = AdminDeA();
        var creado = await LeerAsync(
            await cliente.PostAsJsonAsync("/api/r/resto-a/s/centro/cierres", new { inicio = "2026-12-01T10:00", fin = "2026-12-02T10:00" }),
            HttpStatusCode.Created);
        var id = creado.GetProperty("id").GetGuid();

        var cancelado = await LeerAsync(
            await cliente.PostAsync($"/api/r/resto-a/s/centro/cierres/{id}/cancelar", null), HttpStatusCode.OK);

        Assert.Equal(id, cancelado.GetProperty("id").GetGuid());
        var lista = await LeerAsync(await cliente.GetAsync("/api/r/resto-a/s/centro/cierres"), HttpStatusCode.OK);
        Assert.Empty(lista.EnumerateArray());
        Assert.NotNull(Assert.Single(_fabrica.Cierres.Cierres).CanceladoEn);
    }

    [Fact(DisplayName = "003/RF-11: cancelar un cierre ya cancelado o ya terminado da 404")]
    public async Task Cancelar_cancelado_o_terminado_da_404()
    {
        var yaCancelado = CierreTemporal.Crear(_a.Id, _centro.Id, Ahora, Ahora.AddDays(2), null, Ahora);
        yaCancelado.Cancelar(Ahora.AddMinutes(-1));
        var terminado = CierreTemporal.Crear(_a.Id, _centro.Id, Ahora.AddDays(-3), Ahora.AddMinutes(-5), null, Ahora.AddDays(-4));
        _fabrica.Cierres.Cierres.AddRange([yaCancelado, terminado]);
        var finCancelado = yaCancelado.Fin;
        var cliente = AdminDeA();

        foreach (var cierre in new[] { yaCancelado, terminado })
        {
            var respuesta = await cliente.PostAsync($"/api/r/resto-a/s/centro/cierres/{cierre.Id}/cancelar", null);
            await AssertNoEncontradoAsync(respuesta, "No encontrado", "Cierre no encontrado");
        }

        Assert.Equal(finCancelado, yaCancelado.Fin);
        Assert.Equal(Ahora.AddMinutes(-5), terminado.Fin);
        Assert.Null(terminado.CanceladoEn);
    }

    [Fact(DisplayName = "003/RF-15: cancelar desde la sucursal A un cierre de la B da 404")]
    public async Task Cancelar_de_otra_sucursal_da_404()
    {
        var cliente = AdminDeA();
        var deNorte = await LeerAsync(
            await cliente.PostAsJsonAsync("/api/r/resto-a/s/norte/cierres", new { fin = "2026-12-02T10:00" }),
            HttpStatusCode.Created);
        var id = deNorte.GetProperty("id").GetGuid();

        var desdeCentro = await cliente.PostAsync($"/api/r/resto-a/s/centro/cierres/{id}/cancelar", null);

        await AssertNoEncontradoAsync(desdeCentro, "No encontrado", "Cierre no encontrado");
        var centro = await LeerAsync(await cliente.GetAsync("/api/r/resto-a/s/centro/cierres"), HttpStatusCode.OK);
        var norte = await LeerAsync(await cliente.GetAsync("/api/r/resto-a/s/norte/cierres"), HttpStatusCode.OK);
        Assert.Empty(centro.EnumerateArray());
        Assert.Equal(id, Assert.Single(norte.EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Null(Assert.Single(_fabrica.Cierres.Cierres).CanceladoEn);
    }

    [Fact(DisplayName = "003/RF-15: los cierres de otro restaurante o sucursal inexistente dan 404 Sucursal no disponible")]
    public async Task Sucursal_de_otro_restaurante()
    {
        var cliente = AdminDeA();

        var get = await cliente.GetAsync("/api/r/resto-a/s/solo-de-b/cierres");
        var post = await cliente.PostAsJsonAsync("/api/r/resto-a/s/solo-de-b/cierres", new { fin = "2026-12-02T10:00" });
        var cancelar = await cliente.PostAsync($"/api/r/resto-a/s/solo-de-b/cierres/{Guid.NewGuid()}/cancelar", null);

        foreach (var respuesta in new[] { get, post, cancelar })
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            Assert.Equal("Sucursal no disponible", json.RootElement.GetProperty("title").GetString());
        }

        Assert.Empty(_fabrica.Cierres.Cierres);
    }

    [Fact(DisplayName = "003/RF-06: desactivar y reactivar conserva los cierres")]
    public async Task Desactivar_y_reactivar_conserva_los_cierres()
    {
        var cliente = AdminDeA();
        var creado = await LeerAsync(
            await cliente.PostAsJsonAsync("/api/r/resto-a/s/centro/cierres", new { inicio = "2026-12-01T10:00", fin = "2026-12-02T10:00", motivo = "Evento" }),
            HttpStatusCode.Created);
        var antes = await LeerAsync(await cliente.GetAsync("/api/r/resto-a/s/centro/cierres"), HttpStatusCode.OK);

        var apagada = await cliente.PutAsJsonAsync($"/api/r/resto-a/sucursales/{_centro.Id}/estado", new { activa = false });
        var encendida = await cliente.PutAsJsonAsync($"/api/r/resto-a/sucursales/{_centro.Id}/estado", new { activa = true });

        Assert.Equal(HttpStatusCode.OK, apagada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, encendida.StatusCode);
        var despues = await LeerAsync(await cliente.GetAsync("/api/r/resto-a/s/centro/cierres"), HttpStatusCode.OK);
        Assert.Equal(antes.ToString(), despues.ToString());
        Assert.Equal(creado.GetProperty("id").GetGuid(), Assert.Single(despues.EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Null(Assert.Single(_fabrica.Cierres.Cierres).CanceladoEn);
    }

    [Fact(DisplayName = "003/RF-07: los cierres exigen administrador_general")]
    public async Task Exige_administrador_general()
    {
        var cajero = ClienteComo(Rol.Cajero);

        var get = await cajero.GetAsync("/api/r/resto-a/s/centro/cierres");
        var post = await cajero.PostAsJsonAsync("/api/r/resto-a/s/centro/cierres", new { fin = "2026-12-02T10:00" });
        var cancelar = await cajero.PostAsync($"/api/r/resto-a/s/centro/cierres/{Guid.NewGuid()}/cancelar", null);
        var anonimo = await _fabrica.CreateClient().GetAsync("/api/r/resto-a/s/centro/cierres");

        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cancelar.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
        Assert.Empty(_fabrica.Cierres.Cierres);
    }
}
