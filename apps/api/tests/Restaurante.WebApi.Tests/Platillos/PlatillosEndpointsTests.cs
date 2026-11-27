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

    private Platillo Sembrar(
        Guid restauranteId, Guid categoriaId, string nombre, int orden = 1, string[]? ingredientes = null)
    {
        var platillo = Platillo.Crear(
            restauranteId, categoriaId, new DatosPlatillo(nombre, null, null, orden, ingredientes ?? []), DateTimeOffset.UtcNow);
        _fabrica.Platillos.Platillos.Add(platillo);
        return platillo;
    }

    private static async Task<JsonElement> LeerAsync(HttpResponseMessage respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    private static IEnumerable<string?> Nombres(JsonElement raiz) =>
        raiz.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("nombre").GetString());

    [Fact(DisplayName = "006/RF-05: GET lista los platillos no eliminados de la categoría por orden y nombre")]
    public async Task Listado_trae_los_no_eliminados_de_la_categoria_por_orden_y_nombre()
    {
        Sembrar(_a.Id, _pozoles.Id, "Zeta", orden: 1);
        Sembrar(_a.Id, _pozoles.Id, "Alfa", orden: 2);
        Sembrar(_a.Id, _pozoles.Id, "Beta", orden: 1).Desactivar();
        Sembrar(_a.Id, _pozoles.Id, "Borrado").Eliminar(DateTimeOffset.UtcNow);
        Sembrar(_a.Id, SembrarCategoria(_a.Id, "Postres").Id, "Flan");

        var respuesta = await AdminDeA().GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        Assert.Equal(["Beta", "Zeta", "Alfa"], Nombres(raiz));
        Assert.Equal(3, raiz.GetProperty("total").GetInt32());
        var primero = raiz.GetProperty("elementos")[0];
        Assert.False(primero.GetProperty("activo").GetBoolean());
        Assert.Equal("Pozoles", primero.GetProperty("categoriaNombre").GetString());
    }

    [Fact(DisplayName = "006/RF-05: por defecto página 1 de 20 con el total")]
    public async Task Listado_por_defecto_pagina_1_de_20()
    {
        for (var i = 1; i <= 25; i++)
        {
            Sembrar(_a.Id, _pozoles.Id, $"Plato {i:00}", orden: i);
        }

        var primera = await LeerAsync(await AdminDeA().GetAsync(Ruta));
        Assert.Equal(20, primera.GetProperty("elementos").GetArrayLength());
        Assert.Equal(25, primera.GetProperty("total").GetInt32());
        Assert.Equal(1, primera.GetProperty("pagina").GetInt32());
        Assert.Equal(20, primera.GetProperty("tamano").GetInt32());

        var segunda = await LeerAsync(await AdminDeA().GetAsync($"{Ruta}?pagina=2"));
        Assert.Equal(5, segunda.GetProperty("elementos").GetArrayLength());
        Assert.Equal("Plato 21", segunda.GetProperty("elementos")[0].GetProperty("nombre").GetString());
    }

    [Fact(DisplayName = "006/RF-05: una categoría inactiva lista sus platillos")]
    public async Task Categoria_inactiva_lista_sus_platillos()
    {
        Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");
        _pozoles.Desactivar();

        var respuesta = await AdminDeA().GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(["Pozole rojo"], Nombres(await LeerAsync(respuesta)));
    }

    [Theory(DisplayName = "006/RF-05: categoría inexistente, eliminada o de otro restaurante da 404")]
    [InlineData("inexistente")]
    [InlineData("eliminada")]
    [InlineData("ajena")]
    public async Task Listado_de_categoria_no_disponible_da_404(string caso)
    {
        var id = caso switch
        {
            "eliminada" => EliminadaDeA().Id,
            "ajena" => SembrarCategoria(_b.Id, "Ajena").Id,
            _ => Guid.NewGuid(),
        };

        var respuesta = await AdminDeA().GetAsync(RutaDe(id));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        Assert.Equal("Categoría no encontrada", raiz.GetProperty("detail").GetString());
    }

    [Theory(DisplayName = "006/RF-05: parámetros fuera de rango dan 400")]
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

    [Fact(DisplayName = "006/RF-06: GET de un platillo devuelve su categoría e ingredientes en orden")]
    public async Task Detalle_devuelve_categoria_e_ingredientes_en_orden()
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo", ingredientes: ["Crema", "Cebolla", "Limón"]);

        var respuesta = await AdminDeA().GetAsync($"/api/r/resto-a/platillos/{platillo.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        Assert.Equal(platillo.Id, raiz.GetProperty("id").GetGuid());
        Assert.Equal(_pozoles.Id, raiz.GetProperty("categoriaId").GetGuid());
        Assert.Equal("Pozoles", raiz.GetProperty("categoriaNombre").GetString());
        Assert.Equal("Pozole rojo", raiz.GetProperty("nombre").GetString());
        Assert.Equal(["Crema", "Cebolla", "Limón"], raiz.GetProperty("ingredientes").EnumerateArray().Select(e => e.GetString()));
        Assert.True(raiz.GetProperty("activo").GetBoolean());
    }

    [Theory(DisplayName = "006/RF-10: un platillo inexistente, eliminado o de otro restaurante da 404")]
    [InlineData("inexistente")]
    [InlineData("eliminado")]
    [InlineData("ajeno")]
    public async Task Detalle_no_disponible_da_404(string caso)
    {
        var id = caso switch
        {
            "eliminado" => EliminadoDeA().Id,
            "ajeno" => Sembrar(_b.Id, SembrarCategoria(_b.Id, "Ajena").Id, "Ajeno").Id,
            _ => Guid.NewGuid(),
        };

        var respuesta = await AdminDeA().GetAsync($"/api/r/resto-a/platillos/{id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        Assert.Equal("No encontrado", raiz.GetProperty("title").GetString());
        Assert.Equal("Platillo no encontrado", raiz.GetProperty("detail").GetString());
    }

    private Platillo EliminadoDeA()
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Borrado");
        platillo.Eliminar(DateTimeOffset.UtcNow);
        return platillo;
    }

    [Fact(DisplayName = "006/RF-11: listar y consultar exigen administrador_general")]
    public async Task Listar_y_consultar_con_cajero_dan_403()
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");
        var cajero = ClienteComo(Rol.Cajero, _a.Id);

        var lista = await cajero.GetAsync(Ruta);
        var detalle = await cajero.GetAsync($"/api/r/resto-a/platillos/{platillo.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, lista.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, detalle.StatusCode);
    }

    private static Task<HttpResponseMessage> PutJsonAsync(HttpClient cliente, string ruta, string json) =>
        cliente.PutAsync(ruta, new StringContent(json, Encoding.UTF8, "application/json"));

    private static string RutaPlatillo(Guid id) => $"/api/r/resto-a/platillos/{id}";

    private static string CuerpoEdicion(Guid categoriaId, string nombre = "Pozole verde") =>
        $$"""{"categoriaId":"{{categoriaId}}","nombre":"{{nombre}}","orden":2,"ingredientes":["Cebolla","Limón"]}""";

    private async Task<JsonElement> ObtenerAsync(Guid id) =>
        await LeerAsync(await AdminDeA().GetAsync(RutaPlatillo(id)));

    [Fact(DisplayName = "006/RF-07: PUT edita los datos y los ingredientes")]
    public async Task Editar_cambia_los_datos_y_los_ingredientes()
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo", ingredientes: ["Crema"]);

        var respuesta = await PutJsonAsync(AdminDeA(), RutaPlatillo(platillo.Id), CuerpoEdicion(_pozoles.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        Assert.Equal("Pozole verde", raiz.GetProperty("nombre").GetString());
        Assert.Equal(2, raiz.GetProperty("orden").GetInt32());
        Assert.Equal(["Cebolla", "Limón"], raiz.GetProperty("ingredientes").EnumerateArray().Select(e => e.GetString()));
        var releido = await ObtenerAsync(platillo.Id);
        Assert.Equal("Pozole verde", releido.GetProperty("nombre").GetString());
        Assert.Equal(["Cebolla", "Limón"], releido.GetProperty("ingredientes").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact(DisplayName = "006/RF-07: renombrar cambiando sólo mayúsculas da 200")]
    public async Task Editar_solo_mayusculas_da_200()
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "pozole rojo");

        var respuesta = await PutJsonAsync(
            AdminDeA(), RutaPlatillo(platillo.Id), CuerpoEdicion(_pozoles.Id, "Pozole Rojo"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Pozole Rojo", (await ObtenerAsync(platillo.Id)).GetProperty("nombre").GetString());
    }

    [Fact(DisplayName = "006/RF-07: mover a otra categoría activa cambia categoriaId y categoriaNombre")]
    public async Task Mover_a_otra_categoria_activa()
    {
        var postres = SembrarCategoria(_a.Id, "Postres");
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");

        var respuesta = await PutJsonAsync(AdminDeA(), RutaPlatillo(platillo.Id), CuerpoEdicion(postres.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var releido = await ObtenerAsync(platillo.Id);
        Assert.Equal(postres.Id, releido.GetProperty("categoriaId").GetGuid());
        Assert.Equal("Postres", releido.GetProperty("categoriaNombre").GetString());
    }

    [Fact(DisplayName = "006/RF-07: mover a una categoría inactiva da 400 y quedarse en la actual inactiva da 200")]
    public async Task Mover_a_inactiva_da_400_y_quedarse_en_la_actual_inactiva_da_200()
    {
        var postres = SembrarCategoria(_a.Id, "Postres");
        postres.Desactivar();
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");

        var mover = await PutJsonAsync(AdminDeA(), RutaPlatillo(platillo.Id), CuerpoEdicion(postres.Id));

        await AssertValidacionAsync(mover, "categoriaId", "La categoría está inactiva");
        Assert.Equal(_pozoles.Id, platillo.CategoriaId);

        _pozoles.Desactivar();
        var quedarse = await PutJsonAsync(AdminDeA(), RutaPlatillo(platillo.Id), CuerpoEdicion(_pozoles.Id));

        Assert.Equal(HttpStatusCode.OK, quedarse.StatusCode);
        Assert.Equal("Pozole verde", platillo.Nombre);
    }

    [Theory(DisplayName = "006/RF-07: una categoría inexistente o de otro restaurante da 400 La categoría no existe")]
    [InlineData("inexistente")]
    [InlineData("ajena")]
    public async Task Categoria_inexistente_o_ajena_da_400(string caso)
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");
        var id = caso == "ajena" ? SembrarCategoria(_b.Id, "Ajena").Id : Guid.NewGuid();

        var respuesta = await PutJsonAsync(AdminDeA(), RutaPlatillo(platillo.Id), CuerpoEdicion(id));

        await AssertValidacionAsync(respuesta, "categoriaId", "La categoría no existe");
        Assert.Equal("Pozole rojo", platillo.Nombre);
    }

    [Theory(DisplayName = "006/RF-07: sin categoría o con un identificador inválido da 400 en categoriaId")]
    [InlineData("""{"nombre":"Pozole","orden":1}""", "La categoría es obligatoria")]
    [InlineData("""{"categoriaId":null,"nombre":"Pozole","orden":1}""", "La categoría es obligatoria")]
    [InlineData("""{"categoriaId":"no-es-guid","nombre":"Pozole","orden":1}""", "El cuerpo de la petición no es válido.")]
    public async Task Sin_categoria_o_invalida_da_400(string cuerpo, string mensaje)
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");

        var respuesta = await PutJsonAsync(AdminDeA(), RutaPlatillo(platillo.Id), cuerpo);

        await AssertValidacionAsync(respuesta, "categoriaId", mensaje);
        Assert.Equal("Pozole rojo", platillo.Nombre);
    }

    [Theory(DisplayName = "006/RF-07: editar con datos inválidos da 400 por campo")]
    [InlineData("""{"nombre":"","orden":1}""", "nombre", "El nombre es obligatorio")]
    [InlineData("""{"nombre":"Ok","orden":"3"}""", "orden", "El cuerpo de la petición no es válido.")]
    [InlineData("""{"nombre":"Ok","orden":1,"ingredientes":[" crema ","Crema"]}""", "ingredientes", "El ingrediente 2 está repetido")]
    public async Task Editar_con_datos_invalidos_da_400(string cuerpo, string campo, string mensaje)
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");
        var completo = cuerpo.Replace("{", $$"""{"categoriaId":"{{_pozoles.Id}}",""");

        var respuesta = await PutJsonAsync(AdminDeA(), RutaPlatillo(platillo.Id), completo);

        await AssertValidacionAsync(respuesta, campo, mensaje);
        Assert.Equal("Pozole rojo", platillo.Nombre);
    }

    [Fact(DisplayName = "006/RF-07: nombre de otro platillo da 409 en nombre")]
    public async Task Editar_con_nombre_de_otro_platillo_da_409()
    {
        Sembrar(_a.Id, _pozoles.Id, "Pozole verde");
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");

        var respuesta = await PutJsonAsync(
            AdminDeA(), RutaPlatillo(platillo.Id), CuerpoEdicion(_pozoles.Id, " POZOLE VERDE "));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var errores = (await LeerAsync(respuesta)).GetProperty("errors");
        Assert.Equal(
            ["Ya existe un platillo con ese nombre"],
            errores.GetProperty("nombre").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal("Pozole rojo", platillo.Nombre);
    }

    [Fact(DisplayName = "006/RF-07: nombre corto y descripción en blanco al editar se guardan como null")]
    public async Task Editar_con_corto_y_descripcion_en_blanco_los_quita()
    {
        var platillo = Platillo.Crear(
            _a.Id, _pozoles.Id, new DatosPlatillo("Pozole rojo", "Pozole", "Con maciza", 1, []), DateTimeOffset.UtcNow);
        _fabrica.Platillos.Platillos.Add(platillo);

        var respuesta = await PutJsonAsync(
            AdminDeA(), RutaPlatillo(platillo.Id),
            $$"""{"categoriaId":"{{_pozoles.Id}}","nombre":"Pozole rojo","nombreCorto":"   ","descripcion":"  ","orden":1}""");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var releido = await ObtenerAsync(platillo.Id);
        Assert.Equal(JsonValueKind.Null, releido.GetProperty("nombreCorto").ValueKind);
        Assert.Equal(JsonValueKind.Null, releido.GetProperty("descripcion").ValueKind);
        Assert.Null(platillo.NombreCorto);
        Assert.Null(platillo.Descripcion);
    }

    [Fact(DisplayName = "006/RF-08: desactivar lo mantiene en la lista y con el nombre reservado")]
    public async Task Desactivar_lo_mantiene_en_la_lista_y_con_el_nombre_reservado()
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");
        var cliente = AdminDeA();

        var apagar = await PutJsonAsync(cliente, $"{RutaPlatillo(platillo.Id)}/estado", """{"activo":false}""");

        Assert.Equal(HttpStatusCode.OK, apagar.StatusCode);
        Assert.False((await LeerAsync(apagar)).GetProperty("activo").GetBoolean());
        var lista = await LeerAsync(await cliente.GetAsync(Ruta));
        Assert.Equal(["Pozole rojo"], Nombres(lista));
        Assert.False(lista.GetProperty("elementos")[0].GetProperty("activo").GetBoolean());

        var repetido = await PostJsonAsync(cliente, Ruta, """{"nombre":"pozole rojo","orden":2}""");
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);

        var encender = await PutJsonAsync(cliente, $"{RutaPlatillo(platillo.Id)}/estado", """{"activo":true}""");
        Assert.Equal(HttpStatusCode.OK, encender.StatusCode);
        Assert.True((await LeerAsync(encender)).GetProperty("activo").GetBoolean());
    }

    [Theory(DisplayName = "006/RF-08: estado sin activo o no booleano da 400 en activo")]
    [InlineData("{}", "El estado es obligatorio")]
    [InlineData("""{"activo":null}""", "El estado es obligatorio")]
    [InlineData("""{"activo":"si"}""", "El cuerpo de la petición no es válido.")]
    [InlineData("""{"activo":"true"}""", "El cuerpo de la petición no es válido.")]
    public async Task Estado_invalido_da_400_en_activo(string cuerpo, string mensaje)
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");

        var respuesta = await PutJsonAsync(AdminDeA(), $"{RutaPlatillo(platillo.Id)}/estado", cuerpo);

        await AssertValidacionAsync(respuesta, "activo", mensaje);
        Assert.True(platillo.Activo);
    }

    [Theory(DisplayName = "006/RF-10: editar o cambiar estado de un platillo inexistente, eliminado o de otro restaurante da 404")]
    [InlineData("inexistente")]
    [InlineData("eliminado")]
    [InlineData("ajeno")]
    public async Task Editar_o_cambiar_estado_no_disponible_da_404(string caso)
    {
        var id = caso switch
        {
            "eliminado" => EliminadoDeA().Id,
            "ajeno" => Sembrar(_b.Id, SembrarCategoria(_b.Id, "Ajena").Id, "Ajeno").Id,
            _ => Guid.NewGuid(),
        };
        var cliente = AdminDeA();

        var editar = await PutJsonAsync(cliente, RutaPlatillo(id), CuerpoEdicion(_pozoles.Id));
        var estado = await PutJsonAsync(cliente, $"{RutaPlatillo(id)}/estado", """{"activo":false}""");

        foreach (var respuesta in new[] { editar, estado })
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            var raiz = await LeerAsync(respuesta);
            Assert.Equal("No encontrado", raiz.GetProperty("title").GetString());
            Assert.Equal("Platillo no encontrado", raiz.GetProperty("detail").GetString());
        }
    }

    [Fact(DisplayName = "006/RF-11: editar y cambiar estado exigen administrador_general")]
    public async Task Editar_y_cambiar_estado_con_cajero_dan_403()
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");
        var cajero = ClienteComo(Rol.Cajero, _a.Id);

        var editar = await PutJsonAsync(cajero, RutaPlatillo(platillo.Id), CuerpoEdicion(_pozoles.Id));
        var estado = await PutJsonAsync(cajero, $"{RutaPlatillo(platillo.Id)}/estado", """{"activo":false}""");

        Assert.Equal(HttpStatusCode.Forbidden, editar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, estado.StatusCode);
        Assert.True(platillo.Activo);
    }

    [Fact(DisplayName = "006/RF-09: eliminar responde 204, lo quita de la lista y libera sus nombres")]
    public async Task Eliminar_responde_204_y_libera_nombres()
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");
        var cliente = AdminDeA();

        var respuesta = await cliente.DeleteAsync(RutaPlatillo(platillo.Id));

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(0, respuesta.Content.Headers.ContentLength ?? 0);
        Assert.Empty((await LeerAsync(await cliente.GetAsync(Ruta))).GetProperty("elementos").EnumerateArray());
        var alta = await PostJsonAsync(cliente, Ruta, """{"nombre":"Pozole rojo","orden":1}""");
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        Assert.NotNull(platillo.EliminadaEn);
        Assert.Contains(platillo, _fabrica.Platillos.Platillos);
    }

    [Theory(DisplayName = "006/RF-10: eliminar un platillo inexistente o ya eliminado da 404")]
    [InlineData("inexistente")]
    [InlineData("eliminado")]
    [InlineData("ajeno")]
    public async Task Eliminar_no_visible_da_404(string caso)
    {
        var id = caso switch
        {
            "eliminado" => Eliminado().Id,
            "ajeno" => Sembrar(_b.Id, SembrarCategoria(_b.Id, "Ajena").Id, "Ajeno").Id,
            _ => Guid.NewGuid(),
        };

        var respuesta = await AdminDeA().DeleteAsync(RutaPlatillo(id));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        Assert.Equal("No encontrado", raiz.GetProperty("title").GetString());
        Assert.Equal("Platillo no encontrado", raiz.GetProperty("detail").GetString());

        Platillo Eliminado()
        {
            var p = Sembrar(_a.Id, _pozoles.Id, "Viejo");
            p.Eliminar(DateTimeOffset.UtcNow);
            return p;
        }
    }

    [Fact(DisplayName = "006/RF-11: eliminar exige administrador_general")]
    public async Task Eliminar_con_cajero_da_403()
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");

        var respuesta = await ClienteComo(Rol.Cajero, _a.Id).DeleteAsync(RutaPlatillo(platillo.Id));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Null(platillo.EliminadaEn);
    }

    [Theory(DisplayName = "006/RF-12: eliminar una categoría con platillos da 409 con la cantidad")]
    [InlineData(2, "Esta categoría tiene 2 platillos")]
    [InlineData(1, "Esta categoría tiene 1 platillo")]
    public async Task Eliminar_categoria_con_platillos_da_409(int cantidad, string detalle)
    {
        for (var i = 0; i < cantidad; i++)
        {
            var p = Sembrar(_a.Id, _pozoles.Id, $"Plato {i}", orden: i + 1);
            if (i == 1) p.Desactivar();
        }
        var cliente = AdminDeA();

        var respuesta = await cliente.DeleteAsync($"/api/r/resto-a/categorias/{_pozoles.Id}");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var raiz = await LeerAsync(respuesta);
        Assert.Equal("Conflicto", raiz.GetProperty("title").GetString());
        Assert.Equal(detalle, raiz.GetProperty("detail").GetString());
        Assert.Equal(cantidad, raiz.GetProperty("platillos").GetInt32());
        Assert.Null(_pozoles.EliminadaEn);
        var lista = await LeerAsync(await cliente.GetAsync("/api/r/resto-a/categorias"));
        Assert.Contains("Pozoles", lista.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("nombre").GetString()));
    }

    [Fact(DisplayName = "006/RF-12: tras eliminar su único platillo la categoría se puede eliminar")]
    public async Task Tras_eliminar_el_unico_platillo_la_categoria_se_elimina()
    {
        var platillo = Sembrar(_a.Id, _pozoles.Id, "Pozole rojo");
        var cliente = AdminDeA();
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.DeleteAsync($"/api/r/resto-a/categorias/{_pozoles.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync(RutaPlatillo(platillo.Id))).StatusCode);
        var respuesta = await cliente.DeleteAsync($"/api/r/resto-a/categorias/{_pozoles.Id}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.NotNull(_pozoles.EliminadaEn);
    }
}
