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

    private static async Task<JsonElement> LeerAsync(HttpResponseMessage respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    [Fact(DisplayName = "005/RF-04: GET devuelve las no eliminadas, activas e inactivas, por orden y nombre")]
    public async Task Listado_devuelve_no_eliminadas_por_orden_y_nombre()
    {
        Sembrar(_a.Id, "Postres", orden: 2);
        Sembrar(_a.Id, "Bebidas", orden: 2);
        Sembrar(_a.Id, "Entradas", orden: 1);
        Sembrar(_a.Id, "Oculta", orden: 3);
        _fabrica.Categorias.Categorias[^1].Desactivar();
        Sembrar(_a.Id, "Borrada", orden: 1);
        _fabrica.Categorias.Categorias[^1].Eliminar(DateTimeOffset.UtcNow);

        var respuesta = await AdminDeA().GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        var elementos = raiz.GetProperty("elementos").EnumerateArray().ToList();
        Assert.Equal(["Entradas", "Bebidas", "Postres", "Oculta"], elementos.Select(e => e.GetProperty("nombre").GetString()));
        Assert.True(elementos[0].GetProperty("activa").GetBoolean());
        Assert.False(elementos[3].GetProperty("activa").GetBoolean());
        Assert.Equal(4, raiz.GetProperty("total").GetInt32());
    }

    [Fact(DisplayName = "005/RF-04: por defecto página 1 de 20 con el total")]
    public async Task Listado_por_defecto_pagina_1_de_20()
    {
        for (var i = 1; i <= 25; i++)
        {
            Sembrar(_a.Id, $"Cat {i:00}", orden: i);
        }

        var primera = await LeerAsync(await AdminDeA().GetAsync(Ruta));
        Assert.Equal(20, primera.GetProperty("elementos").GetArrayLength());
        Assert.Equal(25, primera.GetProperty("total").GetInt32());
        Assert.Equal(1, primera.GetProperty("pagina").GetInt32());
        Assert.Equal(20, primera.GetProperty("tamano").GetInt32());

        var segunda = await LeerAsync(await AdminDeA().GetAsync($"{Ruta}?pagina=2"));
        Assert.Equal(5, segunda.GetProperty("elementos").GetArrayLength());
        Assert.Equal(2, segunda.GetProperty("pagina").GetInt32());
        Assert.Equal("Cat 21", segunda.GetProperty("elementos")[0].GetProperty("nombre").GetString());
    }

    [Theory(DisplayName = "005/RF-04: parámetros fuera de rango dan 400")]
    [InlineData("pagina=0", "pagina", "La página debe ser mayor o igual que 1")]
    [InlineData("pagina=abc", "pagina", "El cuerpo de la petición no es válido.")]
    [InlineData("tamano=0", "tamano", "El tamaño debe estar entre 1 y 100")]
    [InlineData("tamano=101", "tamano", "El tamaño debe estar entre 1 y 100")]
    [InlineData("tamano=abc", "tamano", "El cuerpo de la petición no es válido.")]
    public async Task Listado_con_parametros_invalidos_da_400(string consulta, string campo, string mensaje)
    {
        var respuesta = await AdminDeA().GetAsync($"{Ruta}?{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var errores = (await LeerAsync(respuesta)).GetProperty("errors");
        Assert.Equal([campo], errores.EnumerateObject().Select(p => p.Name));
        Assert.Equal([mensaje], errores.GetProperty(campo).EnumerateArray().Select(e => e.GetString()));
    }

    [Fact(DisplayName = "005/RF-04: una página enorme devuelve 200 con elementos vacíos")]
    public async Task Listado_con_pagina_enorme_devuelve_vacio()
    {
        Sembrar(_a.Id, "Bebidas");

        var respuesta = await AdminDeA().GetAsync($"{Ruta}?pagina=2147483647");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        Assert.Equal(0, raiz.GetProperty("elementos").GetArrayLength());
        Assert.Equal(1, raiz.GetProperty("total").GetInt32());
    }

    [Fact(DisplayName = "005/RF-04: un cuerpo JSON inválido conserva la clave cuerpo")]
    public async Task Cuerpo_json_invalido_conserva_la_clave_cuerpo()
    {
        var respuesta = await PostJsonAsync(AdminDeA(), "{no es json");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var errores = (await LeerAsync(respuesta)).GetProperty("errors");
        Assert.Equal(["cuerpo"], errores.EnumerateObject().Select(p => p.Name));
    }

    [Fact(DisplayName = "005/RF-04: no lista categorías de otro restaurante")]
    public async Task Listado_no_incluye_otro_restaurante()
    {
        Sembrar(_a.Id, "Propia");
        Sembrar(_b.Id, "Ajena");

        var raiz = await LeerAsync(await AdminDeA().GetAsync(Ruta));

        Assert.Equal(["Propia"], raiz.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("nombre").GetString()));
        Assert.Equal(1, raiz.GetProperty("total").GetInt32());
    }

    private static Task<HttpResponseMessage> PutJsonAsync(HttpClient cliente, string ruta, string json) =>
        cliente.PutAsync(ruta, new StringContent(json, Encoding.UTF8, "application/json"));

    private Categoria Existente(Guid restauranteId, string nombre, string? corto = null, int orden = 1)
    {
        Sembrar(restauranteId, nombre, corto, orden);
        return _fabrica.Categorias.Categorias[^1];
    }

    [Fact(DisplayName = "005/RF-05: editar cambia nombre, nombre corto y orden")]
    public async Task Editar_cambia_los_datos()
    {
        var c = Existente(_a.Id, "Bebidas", "Beb");

        var respuesta = await PutJsonAsync(AdminDeA(), $"{Ruta}/{c.Id}", """{"nombre":" Refrescos ","nombreCorto":"Ref","orden":4}""");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        Assert.Equal(c.Id, raiz.GetProperty("id").GetGuid());
        Assert.Equal("Refrescos", raiz.GetProperty("nombre").GetString());
        Assert.Equal("Ref", raiz.GetProperty("nombreCorto").GetString());
        Assert.Equal(4, raiz.GetProperty("orden").GetInt32());
        Assert.Equal("Refrescos", c.Nombre);
    }

    [Fact(DisplayName = "005/RF-05: renombrar cambiando sólo mayúsculas no choca consigo misma")]
    public async Task Editar_solo_mayusculas_da_200()
    {
        var c = Existente(_a.Id, "bebidas", "beb");

        var respuesta = await PutJsonAsync(AdminDeA(), $"{Ruta}/{c.Id}", """{"nombre":"Bebidas","nombreCorto":"Beb","orden":1}""");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Bebidas", c.Nombre);
    }

    [Fact(DisplayName = "005/RF-05: editar con el nombre de otra categoría da 409 en nombre")]
    public async Task Editar_con_nombre_ajeno_da_409()
    {
        Existente(_a.Id, "Bebidas");
        var c = Existente(_a.Id, "Postres", orden: 2);

        var respuesta = await PutJsonAsync(AdminDeA(), $"{Ruta}/{c.Id}", """{"nombre":" bebidas ","orden":2}""");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        Assert.Equal("Conflicto", raiz.GetProperty("title").GetString());
        Assert.Equal("Ya existe una categoría con ese nombre", raiz.GetProperty("detail").GetString());
        Assert.Equal(
            ["Ya existe una categoría con ese nombre"],
            raiz.GetProperty("errors").GetProperty("nombre").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal("Postres", c.Nombre);
    }

    [Fact(DisplayName = "005/RF-05: editar con el nombre corto de otra categoría da 409 en nombreCorto")]
    public async Task Editar_con_nombre_corto_ajeno_da_409()
    {
        Existente(_a.Id, "Bebidas", "Beb");
        var c = Existente(_a.Id, "Postres", orden: 2);

        var respuesta = await PutJsonAsync(AdminDeA(), $"{Ruta}/{c.Id}", """{"nombre":"Postres","nombreCorto":"BEB","orden":2}""");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var errores = (await LeerAsync(respuesta)).GetProperty("errors");
        Assert.Equal(["nombreCorto"], errores.EnumerateObject().Select(p => p.Name));
    }

    [Theory(DisplayName = "005/RF-05: editar con datos inválidos da 400 por campo")]
    [InlineData("""{"nombre":"","orden":1}""", "nombre", "El nombre es obligatorio")]
    [InlineData("""{"nombre":"Largo61","orden":1}""", "nombre", "El nombre no puede superar 60 caracteres")]
    [InlineData("""{"nombre":"Ok","nombreCorto":"Largo25","orden":1}""", "nombreCorto", "El nombre corto no puede superar 24 caracteres")]
    [InlineData("""{"nombre":"Ok"}""", "orden", "El orden es obligatorio")]
    [InlineData("""{"nombre":"Ok","orden":0}""", "orden", "El orden debe ser un entero mayor o igual que 1")]
    [InlineData("""{"nombre":"Ok","orden":"3"}""", "orden", "El cuerpo de la petición no es válido.")]
    [InlineData("""{"nombre":"Ok","orden":1.5}""", "orden", "El cuerpo de la petición no es válido.")]
    public async Task Editar_con_datos_invalidos_da_400(string cuerpo, string campo, string mensaje)
    {
        var c = Existente(_a.Id, "Bebidas");
        var json = cuerpo.Replace("Largo61", Largo61).Replace("Largo25", Largo25);

        var respuesta = await PutJsonAsync(AdminDeA(), $"{Ruta}/{c.Id}", json);

        await AssertValidacionAsync(respuesta, campo, mensaje);
        Assert.Equal("Bebidas", c.Nombre);
    }

    [Fact(DisplayName = "005/RF-05: editar con nombre corto en blanco lo quita")]
    public async Task Editar_con_nombre_corto_en_blanco_lo_quita()
    {
        var c = Existente(_a.Id, "Bebidas", "Beb");

        var respuesta = await PutJsonAsync(AdminDeA(), $"{Ruta}/{c.Id}", """{"nombre":"Bebidas","nombreCorto":"   ","orden":1}""");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await LeerAsync(respuesta)).GetProperty("nombreCorto").ValueKind);
        Assert.Null(c.NombreCorto);
    }

    [Fact(DisplayName = "005/RF-06: desactivar mantiene la categoría en la lista y su nombre reservado")]
    public async Task Desactivar_mantiene_lista_y_nombre_reservado()
    {
        var c = Existente(_a.Id, "Bebidas");
        var cliente = AdminDeA();

        var apagar = await PutJsonAsync(cliente, $"{Ruta}/{c.Id}/estado", """{"activa":false}""");
        Assert.Equal(HttpStatusCode.OK, apagar.StatusCode);
        Assert.False((await LeerAsync(apagar)).GetProperty("activa").GetBoolean());

        var lista = await LeerAsync(await cliente.GetAsync(Ruta));
        var elemento = Assert.Single(lista.GetProperty("elementos").EnumerateArray());
        Assert.False(elemento.GetProperty("activa").GetBoolean());

        var alta = await PostJsonAsync(cliente, """{"nombre":"Bebidas","orden":2}""");
        Assert.Equal(HttpStatusCode.Conflict, alta.StatusCode);

        var encender = await PutJsonAsync(cliente, $"{Ruta}/{c.Id}/estado", """{"activa":true}""");
        Assert.Equal(HttpStatusCode.OK, encender.StatusCode);
        Assert.True((await LeerAsync(encender)).GetProperty("activa").GetBoolean());
    }

    [Theory(DisplayName = "005/RF-06: estado sin activa o no booleano da 400 en activa")]
    [InlineData("{}", "El estado es obligatorio")]
    [InlineData("""{"activa":null}""", "El estado es obligatorio")]
    [InlineData("""{"activa":"si"}""", "El cuerpo de la petición no es válido.")]
    [InlineData("""{"activa":"true"}""", "El cuerpo de la petición no es válido.")]
    public async Task Estado_invalido_da_400_en_activa(string cuerpo, string mensaje)
    {
        var c = Existente(_a.Id, "Bebidas");

        var respuesta = await PutJsonAsync(AdminDeA(), $"{Ruta}/{c.Id}/estado", cuerpo);

        await AssertValidacionAsync(respuesta, "activa", mensaje);
        Assert.True(c.Activa);
    }

    [Theory(DisplayName = "005/RF-08: editar o cambiar estado de una categoría de otro restaurante, inexistente o eliminada da 404")]
    [InlineData("ajena", false)]
    [InlineData("inexistente", false)]
    [InlineData("eliminada", false)]
    [InlineData("ajena", true)]
    [InlineData("inexistente", true)]
    [InlineData("eliminada", true)]
    public async Task Categoria_no_visible_da_404(string caso, bool estado)
    {
        var id = caso switch
        {
            "ajena" => Existente(_b.Id, "Ajena").Id,
            "eliminada" => EliminadaDeA().Id,
            _ => Guid.NewGuid()
        };
        var (ruta, cuerpo) = estado
            ? ($"{Ruta}/{id}/estado", """{"activa":false}""")
            : ($"{Ruta}/{id}", """{"nombre":"Nueva","orden":1}""");

        var respuesta = await PutJsonAsync(AdminDeA(), ruta, cuerpo);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        Assert.Equal("No encontrado", raiz.GetProperty("title").GetString());
        Assert.Equal("Categoría no encontrada", raiz.GetProperty("detail").GetString());
    }

    private Categoria EliminadaDeA()
    {
        var c = Existente(_a.Id, "Borrada");
        c.Eliminar(DateTimeOffset.UtcNow);
        return c;
    }

    [Fact(DisplayName = "005/RF-08: el 404 precede al 409 al editar")]
    public async Task Editar_inexistente_con_nombre_repetido_da_404()
    {
        Existente(_a.Id, "Bebidas");

        var respuesta = await PutJsonAsync(AdminDeA(), $"{Ruta}/{Guid.NewGuid()}", """{"nombre":"Bebidas","orden":1}""");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact(DisplayName = "005/RF-09: editar y cambiar estado exigen administrador_general")]
    public async Task Editar_con_cajero_da_403()
    {
        var c = Existente(_a.Id, "Bebidas");
        var cajero = ClienteComo(Rol.Cajero, _a.Id);

        var editar = await PutJsonAsync(cajero, $"{Ruta}/{c.Id}", """{"nombre":"Otra","orden":1}""");
        var estado = await PutJsonAsync(cajero, $"{Ruta}/{c.Id}/estado", """{"activa":false}""");

        Assert.Equal(HttpStatusCode.Forbidden, editar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, estado.StatusCode);
        Assert.Equal("Bebidas", c.Nombre);
        Assert.True(c.Activa);
    }

    [Fact(DisplayName = "005/RF-09: listar exige administrador_general")]
    public async Task Listar_con_cajero_da_403()
    {
        Sembrar(_a.Id, "Bebidas");

        var respuesta = await ClienteComo(Rol.Cajero, _a.Id).GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
