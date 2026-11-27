using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Restaurante.Domain.Menu;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using static Restaurante.WebApi.Tests.Infraestructura.AfirmacionesHttp;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Platillos;

[Collection(ColeccionApi.Nombre)]
public sealed class PlatillosEndpointsTests : IDisposable
{
    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _a;
    private readonly RestauranteEntidad _b;
    private readonly Categoria _pozoles;

    public PlatillosEndpointsTests()
    {
        _a = _fabrica.SembrarRestaurante("Resto A", "resto-a");
        _b = _fabrica.SembrarRestaurante("Resto B", "resto-b");
        _pozoles = SembrarCategoria(_a.Id, "Pozoles");
    }

    public void Dispose() => _fabrica.Dispose();

    private static string RutaDe(Guid categoriaId) => $"/api/r/resto-a/categorias/{categoriaId}/platillos";

    private string Ruta => RutaDe(_pozoles.Id);

    private HttpClient ClienteComo(Rol rol, Guid? restauranteId)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fabrica.EmitirToken(rol, restauranteId: restauranteId));
        return cliente;
    }

    private HttpClient AdminDeA() => ClienteComo(Rol.AdministradorGeneral, _a.Id);

    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient cliente, string ruta, string json) =>
        cliente.PostAsync(ruta, new StringContent(json, Encoding.UTF8, "application/json"));

    private Categoria SembrarCategoria(Guid restauranteId, string nombre)
    {
        var categoria = Categoria.Crear(restauranteId, new DatosCategoria(nombre, null, 1), DateTimeOffset.UtcNow);
        _fabrica.Categorias.Categorias.Add(categoria);
        return categoria;
    }

    private void SembrarPlatillo(Guid restauranteId, Guid categoriaId, string nombre, string? corto = null) =>
        _fabrica.Platillos.Platillos.Add(Platillo.Crear(
            restauranteId, categoriaId, new DatosPlatillo(nombre, corto, null, 1, []), DateTimeOffset.UtcNow));

    private const string CuerpoValido =
        """{"nombre":"Pozole rojo","nombreCorto":"Pozole","descripcion":"Con maciza","orden":1,"ingredientes":["Crema","Cebolla"]}""";

    [Fact(DisplayName = "006/RF-01: POST devuelve 201 con Location y el platillo")]
    public async Task Alta_responde_201_con_location_y_el_platillo()
    {
        var respuesta = await PostJsonAsync(AdminDeA(), Ruta, CuerpoValido);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        var id = raiz.GetProperty("id").GetGuid();
        Assert.Equal(_pozoles.Id, raiz.GetProperty("categoriaId").GetGuid());
        Assert.Equal("Pozoles", raiz.GetProperty("categoriaNombre").GetString());
        Assert.Equal("Pozole rojo", raiz.GetProperty("nombre").GetString());
        Assert.Equal("Pozole", raiz.GetProperty("nombreCorto").GetString());
        Assert.Equal("Con maciza", raiz.GetProperty("descripcion").GetString());
        Assert.Equal(1, raiz.GetProperty("orden").GetInt32());
        Assert.Equal(["Crema", "Cebolla"], raiz.GetProperty("ingredientes").EnumerateArray().Select(e => e.GetString()));
        Assert.True(raiz.GetProperty("activo").GetBoolean());
        Assert.Equal($"/api/r/resto-a/platillos/{id}", respuesta.Headers.Location?.ToString());
        var guardado = Assert.Single(_fabrica.Platillos.Platillos);
        Assert.Equal(_a.Id, guardado.RestauranteId);
        Assert.Equal(id, guardado.Id);
    }

    [Fact(DisplayName = "006/RF-01: sin ingredientes se guarda una lista vacía")]
    public async Task SinIngredientes_se_guarda_una_lista_vacia()
    {
        var respuesta = await PostJsonAsync(AdminDeA(), Ruta, """{"nombre":"Pozole","orden":1}""");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("ingredientes").ValueKind);
        Assert.Equal(0, json.RootElement.GetProperty("ingredientes").GetArrayLength());
        Assert.Empty(Assert.Single(_fabrica.Platillos.Platillos).Ingredientes);
    }

    private static readonly string Largo61 = new('a', 61);
    private static readonly string Largo25 = new('a', 25);
    private static readonly string Largo301 = new('a', 301);
    private static readonly string Largo41 = new('a', 41);
    private static readonly string Treinta1 =
        string.Join(",", Enumerable.Range(1, 31).Select(i => $"\"i{i}\""));

    [Theory(DisplayName = "006/RF-02: datos inválidos dan 400 por campo")]
    [InlineData("""{"nombre":"","orden":1}""", "nombre", "El nombre es obligatorio")]
    [InlineData("""{"nombre":"   ","orden":1}""", "nombre", "El nombre es obligatorio")]
    [InlineData("""{"nombre":"Largo61","orden":1}""", "nombre", "El nombre no puede superar 60 caracteres")]
    [InlineData("""{"nombre":"Ok","nombreCorto":"Largo25","orden":1}""", "nombreCorto", "El nombre corto no puede superar 24 caracteres")]
    [InlineData("""{"nombre":"Ok","descripcion":"Largo301","orden":1}""", "descripcion", "La descripción no puede superar 300 caracteres")]
    [InlineData("""{"nombre":"Ok"}""", "orden", "El orden es obligatorio")]
    [InlineData("""{"nombre":"Ok","orden":0}""", "orden", "El orden debe ser un entero mayor o igual que 1")]
    [InlineData("""{"nombre":"Ok","orden":1.5}""", "orden", "El cuerpo de la petición no es válido.")]
    [InlineData("""{"nombre":"Ok","orden":"3"}""", "orden", "El cuerpo de la petición no es válido.")]
    [InlineData("""{"nombre":"Ok","orden":1,"ingredientes":[Treinta1]}""", "ingredientes", "No puede haber más de 30 ingredientes")]
    [InlineData("""{"nombre":"Ok","orden":1,"ingredientes":["Sal","  "]}""", "ingredientes", "El ingrediente 2 está vacío")]
    [InlineData("""{"nombre":"Ok","orden":1,"ingredientes":["Largo41"]}""", "ingredientes", "El ingrediente 1 no puede superar 40 caracteres")]
    [InlineData("""{"nombre":"Ok","orden":1,"ingredientes":[" crema ","Crema"]}""", "ingredientes", "El ingrediente 2 está repetido")]
    public async Task DatosInvalidos_dan_400_por_campo(string cuerpo, string campo, string mensaje)
    {
        var json = cuerpo.Replace("Largo61", Largo61).Replace("Largo25", Largo25)
            .Replace("Largo301", Largo301).Replace("Largo41", Largo41).Replace("Treinta1", Treinta1);

        var respuesta = await PostJsonAsync(AdminDeA(), Ruta, json);

        await AssertValidacionAsync(respuesta, campo, mensaje);
        Assert.Empty(_fabrica.Platillos.Platillos);
    }

    [Theory(DisplayName = "006/RF-03: una categoría inexistente, eliminada o de otro restaurante da 404")]
    [InlineData("inexistente")]
    [InlineData("eliminada")]
    [InlineData("ajena")]
    public async Task CategoriaNoDisponible_da_404(string caso)
    {
        var id = caso switch
        {
            "eliminada" => EliminadaDeA().Id,
            "ajena" => SembrarCategoria(_b.Id, "Ajena").Id,
            _ => Guid.NewGuid(),
        };

        var respuesta = await PostJsonAsync(AdminDeA(), RutaDe(id), CuerpoValido);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("No encontrado", json.RootElement.GetProperty("title").GetString());
        Assert.Equal("Categoría no encontrada", json.RootElement.GetProperty("detail").GetString());
        Assert.Empty(_fabrica.Platillos.Platillos);
    }

    private Categoria EliminadaDeA()
    {
        var categoria = SembrarCategoria(_a.Id, "Borrada");
        categoria.Eliminar(DateTimeOffset.UtcNow);
        return categoria;
    }

    [Fact(DisplayName = "006/RF-03: una categoría inactiva da 400 en categoriaId")]
    public async Task CategoriaInactiva_da_400_en_categoriaId()
    {
        _pozoles.Desactivar();

        var respuesta = await PostJsonAsync(AdminDeA(), Ruta, CuerpoValido);

        await AssertValidacionAsync(respuesta, "categoriaId", "La categoría está inactiva");
        Assert.Empty(_fabrica.Platillos.Platillos);
    }

    [Fact(DisplayName = "006/RF-04: nombre y nombre corto repetidos dan 409 con los dos campos")]
    public async Task NombreYCortoRepetidos_dan_409_con_los_dos_campos()
    {
        SembrarPlatillo(_a.Id, _pozoles.Id, "Pozole rojo", "Pozole");

        var respuesta = await PostJsonAsync(
            AdminDeA(), Ruta, """{"nombre":" POZOLE ROJO ","nombreCorto":" pozole ","orden":2}""");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        Assert.Equal("Conflicto", raiz.GetProperty("title").GetString());
        var errores = raiz.GetProperty("errors");
        Assert.Equal(
            ["Ya existe un platillo con ese nombre"],
            errores.GetProperty("nombre").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(
            ["Ya existe un platillo con ese nombre corto"],
            errores.GetProperty("nombreCorto").EnumerateArray().Select(e => e.GetString()));
        Assert.Single(_fabrica.Platillos.Platillos);
    }

    [Fact(DisplayName = "006/RF-04: un platillo puede llamarse como una categoría")]
    public async Task Platillo_puede_llamarse_como_una_categoria()
    {
        var respuesta = await PostJsonAsync(AdminDeA(), Ruta, """{"nombre":"Pozoles","orden":1}""");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact(DisplayName = "006/RF-04: el mismo nombre en otro restaurante se acepta")]
    public async Task MismoNombre_en_otro_restaurante_se_acepta()
    {
        var ajena = SembrarCategoria(_b.Id, "Pozoles");
        SembrarPlatillo(_b.Id, ajena.Id, "Pozole rojo", "Pozole");

        var respuesta = await PostJsonAsync(AdminDeA(), Ruta, CuerpoValido);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(2, _fabrica.Platillos.Platillos.Count);
    }

    [Theory(DisplayName = "006/RF-11: un rol distinto de administrador_general recibe 403")]
    [InlineData(Rol.Cajero)]
    [InlineData(Rol.Cocinero)]
    [InlineData(Rol.AdministradorSucursal)]
    [InlineData(Rol.AdministradorPlataforma)]
    public async Task Otro_rol_recibe_403(Rol rol)
    {
        var restauranteId = rol == Rol.AdministradorPlataforma ? (Guid?)null : _a.Id;

        var respuesta = await PostJsonAsync(ClienteComo(rol, restauranteId), Ruta, CuerpoValido);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Empty(_fabrica.Platillos.Platillos);
    }
}
