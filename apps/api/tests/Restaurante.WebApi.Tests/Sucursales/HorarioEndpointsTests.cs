using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using static Restaurante.WebApi.Tests.Infraestructura.AfirmacionesHttp;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Sucursales;

[Collection(ColeccionApi.Nombre)]
public sealed class HorarioEndpointsTests : IDisposable
{
    private static readonly string[] Dias = ["lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo"];

    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _a;
    private readonly RestauranteEntidad _b;

    public HorarioEndpointsTests()
    {
        _a = _fabrica.SembrarRestaurante("Resto A", "resto-a");
        _b = _fabrica.SembrarRestaurante("Resto B", "resto-b");
        Sembrar(_a.Id, "centro", "+525511111111", "C");
        Sembrar(_a.Id, "norte", "+525522222222", "N");
        Sembrar(_b.Id, "solo-de-b", "+525533333333", "B");
    }

    public void Dispose() => _fabrica.Dispose();

    private void Sembrar(Guid restauranteId, string slug, string whatsApp, string prefijo)
    {
        var sucursal = Sucursal.Crear(
            restauranteId,
            slug,
            new DatosSucursal("Sucursal " + slug, "Calle 1", "+525500000000", whatsApp, null, prefijo, ZonasHorarias.PorDefecto),
            DateTimeOffset.UtcNow);
        _fabrica.Sucursales.Sucursales.Add(sucursal);
        _fabrica.Sucursales.Horarios.AddRange(HorarioDia.SemanaCerrada(restauranteId, sucursal.Id));
    }

    private HttpClient ClienteComo(Rol rol)
    {
        // El rol sin alcance general tiene asignada "centro": su 403 viene del rol y no del 404 de 004/RF-15.
        var centro = _fabrica.Sucursales.Sucursales.Single(s => s.RestauranteId == _a.Id && s.Slug == "centro");
        var usuario = Usuario.Crear(
            _a.Id, $"u-{Guid.NewGuid():N}", "sin-hash", rol,
            Usuario.CardinalidadValida(rol, 0) ? [] : [centro.Id], DateTimeOffset.UtcNow);
        _fabrica.Usuarios.Agregar(usuario);
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", _fabrica.EmitirToken(rol, usuarioId: usuario.Id, restauranteId: _a.Id));
        return cliente;
    }

    private HttpClient AdminDeA() => ClienteComo(Rol.AdministradorGeneral);

    private static object[] Semana(string apertura, string cierre) =>
        Dias.Select(d => (object)new { dia = d, cerrado = false, apertura, cierre }).ToArray();

    private static async Task<JsonElement[]> DiasDeAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.EnumerateArray().Select(e => e.Clone()).ToArray();
    }

    [Fact(DisplayName = "003/RF-08: guardar reemplaza los siete días y GET los devuelve en orden")]
    public async Task Guardar_y_consultar_en_orden()
    {
        var cliente = AdminDeA();
        var cuerpo = new
        {
            dias = Dias.Reverse().Select(d => d == "domingo"
                ? (object)new { dia = d, cerrado = true, apertura = "09:00", cierre = "10:00" }
                : new { dia = d, cerrado = false, apertura = "09:00", cierre = "22:00" }).ToArray(),
        };

        var put = await cliente.PutAsJsonAsync("/api/r/resto-a/s/centro/horario", cuerpo);
        var guardado = await DiasDeAsync(put);
        var consultado = await DiasDeAsync(await cliente.GetAsync("/api/r/resto-a/s/centro/horario"));

        Assert.Equal(Dias, consultado.Select(d => d.GetProperty("dia").GetString()));
        Assert.Equal(guardado.Select(d => d.ToString()), consultado.Select(d => d.ToString()));
        Assert.Equal("09:00", consultado[0].GetProperty("apertura").GetString());
        Assert.Equal("22:00", consultado[0].GetProperty("cierre").GetString());
        Assert.False(consultado[0].GetProperty("cerrado").GetBoolean());
        Assert.True(consultado[6].GetProperty("cerrado").GetBoolean());
        Assert.Equal(JsonValueKind.Null, consultado[6].GetProperty("apertura").ValueKind);
        Assert.Equal(JsonValueKind.Null, consultado[6].GetProperty("cierre").ValueKind);
    }

    [Fact(DisplayName = "003/RF-08: GET de una sucursal sin horario guardado devuelve los siete días cerrados")]
    public async Task Consultar_semana_cerrada()
    {
        var consultado = await DiasDeAsync(await AdminDeA().GetAsync("/api/r/resto-a/s/centro/horario"));

        Assert.Equal(7, consultado.Length);
        Assert.All(consultado, d => Assert.True(d.GetProperty("cerrado").GetBoolean()));
    }

    [Fact(DisplayName = "003/RF-09: un cierre anterior a la apertura se acepta como turno nocturno")]
    public async Task Turno_nocturno()
    {
        var cliente = AdminDeA();

        var put = await cliente.PutAsJsonAsync("/api/r/resto-a/s/centro/horario", new { dias = Semana("18:00", "02:00") });
        var consultado = await DiasDeAsync(await cliente.GetAsync("/api/r/resto-a/s/centro/horario"));

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.All(consultado, d =>
        {
            Assert.Equal("18:00", d.GetProperty("apertura").GetString());
            Assert.Equal("02:00", d.GetProperty("cierre").GetString());
        });
    }

    [Fact(DisplayName = "003/RF-09: seis días da 400 en la clave dias")]
    public async Task Faltan_dias()
    {
        var respuesta = await AdminDeA().PutAsJsonAsync(
            "/api/r/resto-a/s/centro/horario", new { dias = Semana("09:00", "22:00").Take(6).ToArray() });

        await AssertValidacionAsync(respuesta, "dias", "El horario debe incluir los siete días");
    }

    [Fact(DisplayName = "003/RF-09: sin cuerpo de días da 400 en la clave dias")]
    public async Task Sin_dias()
    {
        var respuesta = await AdminDeA().PutAsJsonAsync("/api/r/resto-a/s/centro/horario", new { });

        await AssertValidacionAsync(respuesta, "dias", "El horario debe incluir los siete días");
    }

    [Theory(DisplayName = "003/RF-09: horas iguales, ausentes o mal escritas dan 400 en dias[i].campo")]
    [InlineData(2, "10:00", "10:00", "dias[2].cierre", "La hora de apertura y la de cierre no pueden ser iguales")]
    [InlineData(3, "9:00", "22:00", "dias[3].apertura", "La hora debe tener el formato HH:mm")]
    [InlineData(4, "09:00", "25:00", "dias[4].cierre", "La hora debe tener el formato HH:mm")]
    [InlineData(5, "09:00", null, "dias[5].cierre", "Un día abierto necesita hora de apertura y de cierre")]
    [InlineData(6, null, "22:00", "dias[6].apertura", "Un día abierto necesita hora de apertura y de cierre")]
    public async Task Horas_invalidas(int indice, string? apertura, string? cierre, string clave, string mensaje)
    {
        var dias = Semana("09:00", "22:00");
        dias[indice] = new { dia = Dias[indice], cerrado = false, apertura, cierre };

        var respuesta = await AdminDeA().PutAsJsonAsync("/api/r/resto-a/s/centro/horario", new { dias });

        await AssertValidacionAsync(respuesta, clave, mensaje);
        var intacto = await DiasDeAsync(await AdminDeA().GetAsync("/api/r/resto-a/s/centro/horario"));
        Assert.All(intacto, d => Assert.True(d.GetProperty("cerrado").GetBoolean()));
    }

    [Fact(DisplayName = "003/RF-15: el horario de la sucursal A no se ve desde la ruta de la B")]
    public async Task Horario_aislado_por_sucursal()
    {
        var cliente = AdminDeA();
        await cliente.PutAsJsonAsync("/api/r/resto-a/s/norte/horario", new { dias = Semana("09:00", "22:00") });

        var guardadoEnNorte = await DiasDeAsync(await cliente.GetAsync("/api/r/resto-a/s/norte/horario"));
        var centro = await DiasDeAsync(await cliente.GetAsync("/api/r/resto-a/s/centro/horario"));

        Assert.All(guardadoEnNorte, d => Assert.False(d.GetProperty("cerrado").GetBoolean()));
        Assert.Equal(7, centro.Length);
        Assert.All(centro, d => Assert.True(d.GetProperty("cerrado").GetBoolean()));
    }

    [Fact(DisplayName = "003/RF-15: guardar en A no modifica las filas de la B")]
    public async Task Guardar_no_toca_otra_sucursal()
    {
        var cliente = AdminDeA();

        await cliente.PutAsJsonAsync("/api/r/resto-a/s/norte/horario", new { dias = Semana("09:00", "22:00") });

        var centro = _fabrica.Sucursales.Sucursales.Single(s => s.Slug == "centro");
        Assert.All(
            _fabrica.Sucursales.Horarios.Where(h => h.SucursalId == centro.Id),
            h => Assert.True(h.Cerrado));
    }

    [Fact(DisplayName = "003/RF-15: la sucursal de otro restaurante en la ruta da 404 Sucursal no disponible")]
    public async Task Sucursal_de_otro_restaurante()
    {
        var cliente = AdminDeA();

        var get = await cliente.GetAsync("/api/r/resto-a/s/solo-de-b/horario");
        var put = await cliente.PutAsJsonAsync("/api/r/resto-a/s/solo-de-b/horario", new { dias = Semana("09:00", "22:00") });

        foreach (var respuesta in new[] { get, put })
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            Assert.Equal("Sucursal no disponible", json.RootElement.GetProperty("title").GetString());
        }

        var b = _fabrica.Sucursales.Sucursales.Single(s => s.Slug == "solo-de-b");
        Assert.All(_fabrica.Sucursales.Horarios.Where(h => h.SucursalId == b.Id), h => Assert.True(h.Cerrado));
    }

    [Fact(DisplayName = "003/RF-07: guardar el horario exige administrador_general")]
    public async Task Exige_administrador_general()
    {
        var cajero = ClienteComo(Rol.Cajero);

        var put = await cajero.PutAsJsonAsync("/api/r/resto-a/s/centro/horario", new { dias = Semana("09:00", "22:00") });
        var get = await cajero.GetAsync("/api/r/resto-a/s/centro/horario");
        var anonimo = await _fabrica.CreateClient().GetAsync("/api/r/resto-a/s/centro/horario");

        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
        Assert.All(_fabrica.Sucursales.Horarios, h => Assert.True(h.Cerrado));
    }
}
