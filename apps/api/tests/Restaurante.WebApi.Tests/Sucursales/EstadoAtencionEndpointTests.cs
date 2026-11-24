using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;
using Restaurante.WebApi.Tests.Infraestructura;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Sucursales;

[Collection(ColeccionApi.Nombre)]
public sealed class EstadoAtencionEndpointTests : IDisposable
{
    // 2026-11-24 (martes) 18:00Z = 12:00 en Ciudad de México (UTC-6).
    private static readonly DateTimeOffset Ahora = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    private readonly FabricaApi _fabrica;
    private readonly RestauranteEntidad _a;
    private readonly Sucursal _abierta;
    private readonly Sucursal _cerrada;
    private readonly Sucursal _inactiva;

    public EstadoAtencionEndpointTests()
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
        _abierta = Sembrar("abierta", "+525511111111", "A", DiaSemana.Martes);
        _cerrada = Sembrar("cerrada", "+525522222222", "C", DiaSemana.Miercoles);
        _inactiva = Sembrar("inactiva", "+525533333333", "I", DiaSemana.Martes);
        _inactiva.Desactivar();
    }

    public void Dispose() => _fabrica.Dispose();

    private Sucursal Sembrar(string slug, string whatsApp, string prefijo, DiaSemana diaAbierto)
    {
        var sucursal = Sucursal.Crear(
            _a.Id,
            slug,
            new DatosSucursal("Sucursal " + slug, "Calle 1", "+525500000000", whatsApp, null, prefijo, ZonasHorarias.PorDefecto),
            Ahora);
        _fabrica.Sucursales.Sucursales.Add(sucursal);
        _fabrica.Sucursales.Horarios.AddRange(
            Enum.GetValues<DiaSemana>().Select(d => d == diaAbierto
                ? HorarioDia.Crear(_a.Id, sucursal.Id, d, new TimeOnly(9, 0), new TimeOnly(18, 0))
                : HorarioDia.Crear(_a.Id, sucursal.Id, d, null, null)));
        return sucursal;
    }

    private static async Task<JsonElement> LeerAsync(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        Assert.Equal(esperado, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    [Fact(DisplayName = "003/RF-12: el estado de una sucursal abierta responde abierta sin token")]
    public async Task Abierta_sin_token()
    {
        var json = await LeerAsync(
            await _fabrica.CreateClient().GetAsync("/api/r/resto-a/s/abierta/estado"), HttpStatusCode.OK);

        Assert.True(json.GetProperty("abierta").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("mensaje").ValueKind);
    }

    [Fact(DisplayName = "003/RF-13: cerrada devuelve el mensaje con la próxima apertura")]
    public async Task Cerrada_con_mensaje()
    {
        var json = await LeerAsync(
            await _fabrica.CreateClient().GetAsync("/api/r/resto-a/s/cerrada/estado"), HttpStatusCode.OK);

        Assert.False(json.GetProperty("abierta").GetBoolean());
        Assert.Equal("Estamos cerrados, abrimos mañana a las 09:00", json.GetProperty("mensaje").GetString());
    }

    [Fact(DisplayName = "003/RF-14: una sucursal inactiva da 404 Sucursal no disponible")]
    public async Task Inactiva_404()
    {
        var json = await LeerAsync(
            await _fabrica.CreateClient().GetAsync("/api/r/resto-a/s/inactiva/estado"), HttpStatusCode.NotFound);

        Assert.Equal("Sucursal no disponible", json.GetProperty("title").GetString());
    }

    [Fact(DisplayName = "003/RF-14: una sucursal inexistente da 404 Sucursal no disponible")]
    public async Task Inexistente_404()
    {
        var json = await LeerAsync(
            await _fabrica.CreateClient().GetAsync("/api/r/resto-a/s/fantasma/estado"), HttpStatusCode.NotFound);

        Assert.Equal("Sucursal no disponible", json.GetProperty("title").GetString());
    }
}
