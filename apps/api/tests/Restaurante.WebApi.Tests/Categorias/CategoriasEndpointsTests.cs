using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Restaurante.Domain.Menu;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using static Restaurante.WebApi.Tests.Infraestructura.AfirmacionesHttp;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Categorias;

[Collection(ColeccionApi.Nombre)]
public sealed class CategoriasEndpointsTests : IDisposable
{
    private const string Ruta = "/api/r/resto-a/categorias";

    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _a;
    private readonly RestauranteEntidad _b;

    public CategoriasEndpointsTests()
    {
        _a = _fabrica.SembrarRestaurante("Resto A", "resto-a");
        _b = _fabrica.SembrarRestaurante("Resto B", "resto-b");
    }

    public void Dispose() => _fabrica.Dispose();

    private HttpClient ClienteComo(Rol rol, Guid? restauranteId)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fabrica.EmitirToken(rol, restauranteId: restauranteId));
        return cliente;
    }

    private HttpClient AdminDeA() => ClienteComo(Rol.AdministradorGeneral, _a.Id);

    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient cliente, string json) =>
        cliente.PostAsync(Ruta, new StringContent(json, Encoding.UTF8, "application/json"));

    private void Sembrar(Guid restauranteId, string nombre, string? corto = null, int orden = 1) =>
        _fabrica.Categorias.Categorias.Add(
            Categoria.Crear(restauranteId, new DatosCategoria(nombre, corto, orden), DateTimeOffset.UtcNow));

    [Fact(DisplayName = "005/RF-01: POST devuelve 201 con Location y la categoría")]
    public async Task Alta_responde_201_con_location_y_la_categoria()
    {
        var respuesta = await PostJsonAsync(AdminDeA(), """{"nombre":"Pozoles","nombreCorto":"Pozole","orden":1}""");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        var id = raiz.GetProperty("id").GetGuid();
        Assert.Equal("Pozoles", raiz.GetProperty("nombre").GetString());
        Assert.Equal("Pozole", raiz.GetProperty("nombreCorto").GetString());
        Assert.Equal(1, raiz.GetProperty("orden").GetInt32());
        Assert.True(raiz.GetProperty("activa").GetBoolean());
        Assert.Equal($"{Ruta}/{id}", respuesta.Headers.Location?.ToString());
        var guardada = Assert.Single(_fabrica.Categorias.Categorias);
        Assert.Equal(_a.Id, guardada.RestauranteId);
        Assert.Equal(id, guardada.Id);
    }

    private static readonly string Largo61 = new('a', 61);
    private static readonly string Largo25 = new('a', 25);

    [Theory(DisplayName = "005/RF-02: datos inválidos dan 400 por campo")]
    [InlineData("""{"nombre":"","orden":1}""", "nombre", "El nombre es obligatorio")]
    [InlineData("""{"nombre":"   ","orden":1}""", "nombre", "El nombre es obligatorio")]
    [InlineData("""{"nombre":"Largo61","orden":1}""", "nombre", "El nombre no puede superar 60 caracteres")]
    [InlineData("""{"nombre":"Ok","nombreCorto":"Largo25","orden":1}""", "nombreCorto", "El nombre corto no puede superar 24 caracteres")]
    [InlineData("""{"nombre":"Ok"}""", "orden", "El orden es obligatorio")]
    [InlineData("""{"nombre":"Ok","orden":0}""", "orden", "El orden debe ser un entero mayor o igual que 1")]
    [InlineData("""{"nombre":"Ok","orden":-1}""", "orden", "El orden debe ser un entero mayor o igual que 1")]
    [InlineData("""{"nombre":"Ok","orden":1.5}""", "orden", "El cuerpo de la petición no es válido.")]
    [InlineData("""{"nombre":"Ok","orden":"3"}""", "orden", "El cuerpo de la petición no es válido.")]
    [InlineData("""{"nombre":"Ok","orden":"abc"}""", "orden", "El cuerpo de la petición no es válido.")]
    public async Task DatosInvalidos_dan_400_por_campo(string cuerpo, string campo, string mensaje)
    {
        var json = cuerpo.Replace("Largo61", Largo61).Replace("Largo25", Largo25);

        var respuesta = await PostJsonAsync(AdminDeA(), json);

        await AssertValidacionAsync(respuesta, campo, mensaje);
        Assert.Empty(_fabrica.Categorias.Categorias);
    }

    [Fact(DisplayName = "005/RF-02: un nombre corto en blanco se guarda como ausente")]
    public async Task NombreCortoEnBlanco_se_guarda_como_ausente()
    {
        var respuesta = await PostJsonAsync(AdminDeA(), """{"nombre":"Postres","nombreCorto":"  ","orden":2}""");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("nombreCorto").ValueKind);
        Assert.Null(Assert.Single(_fabrica.Categorias.Categorias).NombreCorto);
    }

    [Fact(DisplayName = "005/RF-03: nombre repetido sin distinguir mayúsculas ni espacios da 409 en nombre")]
    public async Task NombreRepetido_da_409_en_nombre()
    {
        Sembrar(_a.Id, "Bebidas");

        var respuesta = await PostJsonAsync(AdminDeA(), """{"nombre":" bebidas ","nombreCorto":"Beb","orden":2}""");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        Assert.Equal(409, raiz.GetProperty("status").GetInt32());
        Assert.Equal("Conflicto", raiz.GetProperty("title").GetString());
        Assert.Equal("Ya existe una categoría con ese nombre", raiz.GetProperty("detail").GetString());
        var errores = raiz.GetProperty("errors");
        Assert.Equal(
            ["Ya existe una categoría con ese nombre"],
            errores.GetProperty("nombre").EnumerateArray().Select(e => e.GetString()));
        Assert.False(errores.TryGetProperty("nombreCorto", out _));
        Assert.Single(_fabrica.Categorias.Categorias);
    }

    [Fact(DisplayName = "005/RF-03: nombre y nombre corto repetidos dan 409 con los dos campos")]
    public async Task NombreYCortoRepetidos_dan_409_con_los_dos_campos()
    {
        Sembrar(_a.Id, "Bebidas", "Beb");

        var respuesta = await PostJsonAsync(AdminDeA(), """{"nombre":"BEBIDAS","nombreCorto":" beb ","orden":2}""");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        Assert.Equal("Conflicto", raiz.GetProperty("title").GetString());
        Assert.Equal(
            "Ya existe una categoría con ese nombre. Ya existe una categoría con ese nombre corto",
            raiz.GetProperty("detail").GetString());
        var errores = raiz.GetProperty("errors");
        Assert.Equal(
            ["Ya existe una categoría con ese nombre"],
            errores.GetProperty("nombre").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(
            ["Ya existe una categoría con ese nombre corto"],
            errores.GetProperty("nombreCorto").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact(DisplayName = "005/RF-03: el mismo nombre en otro restaurante o en una categoría eliminada se acepta")]
    public async Task MismoNombre_en_otro_restaurante_o_eliminada_se_acepta()
    {
        Sembrar(_b.Id, "Bebidas");
        Sembrar(_a.Id, "Postres");
        _fabrica.Categorias.Categorias[^1].Eliminar(DateTimeOffset.UtcNow);

        var bebidas = await PostJsonAsync(AdminDeA(), """{"nombre":"Bebidas","orden":1}""");
        var postres = await PostJsonAsync(AdminDeA(), """{"nombre":"Postres","orden":2}""");

        Assert.Equal(HttpStatusCode.Created, bebidas.StatusCode);
        Assert.Equal(HttpStatusCode.Created, postres.StatusCode);
    }

    [Theory(DisplayName = "005/RF-09: un rol distinto de administrador_general recibe 403")]
    [InlineData(Rol.Cajero)]
    [InlineData(Rol.Cocinero)]
    [InlineData(Rol.AdministradorSucursal)]
    public async Task Otro_rol_recibe_403(Rol rol)
    {
        var respuesta = await PostJsonAsync(ClienteComo(rol, _a.Id), """{"nombre":"Bebidas","orden":1}""");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Empty(_fabrica.Categorias.Categorias);
    }

    [Fact(DisplayName = "005/RF-09: un token de plataforma recibe 403")]
    public async Task Token_de_plataforma_recibe_403()
    {
        var respuesta = await PostJsonAsync(
            ClienteComo(Rol.AdministradorPlataforma, null), """{"nombre":"Bebidas","orden":1}""");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Empty(_fabrica.Categorias.Categorias);
    }
}
