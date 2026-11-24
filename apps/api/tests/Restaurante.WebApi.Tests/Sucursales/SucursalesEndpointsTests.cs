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
public sealed class SucursalesEndpointsTests : IDisposable
{
    private const string Ruta = "/api/r/resto-a/sucursales";

    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _a;
    private readonly RestauranteEntidad _b;

    public SucursalesEndpointsTests()
    {
        _a = _fabrica.SembrarRestaurante("Resto A", "resto-a");
        _b = _fabrica.SembrarRestaurante("Resto B", "resto-b");
    }

    public void Dispose() => _fabrica.Dispose();

    private HttpClient ClienteComo(Rol rol, Guid? restauranteId)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FabricaApi.EmitirToken(rol, restauranteId: restauranteId));
        return cliente;
    }

    private HttpClient AdminDeA() => ClienteComo(Rol.AdministradorGeneral, _a.Id);

    private static Dictionary<string, object?> Cuerpo(
        string nombre = "Centro",
        string slug = "centro",
        string telefono = "+52 55 1234 5678",
        string whatsApp = "+525512345679",
        string? phoneNumberId = null,
        string prefijo = "C",
        string? zonaHoraria = null) => new()
        {
            ["nombre"] = nombre,
            ["direccion"] = "Av. Juárez 1",
            ["telefono"] = telefono,
            ["slug"] = slug,
            ["whatsApp"] = whatsApp,
            ["phoneNumberId"] = phoneNumberId,
            ["prefijo"] = prefijo,
            ["zonaHoraria"] = zonaHoraria,
        };

    private void Sembrar(Guid restauranteId, string slug, string nombre, string whatsApp, string prefijo, string? phoneNumberId = null) =>
        _fabrica.Sucursales.Sucursales.Add(Sucursal.Crear(
            restauranteId,
            slug,
            new DatosSucursal(nombre, "Calle 1", "+525500000000", whatsApp, phoneNumberId, prefijo, ZonasHorarias.PorDefecto),
            DateTimeOffset.UtcNow));

    [Fact(DisplayName = "003/RF-01: POST devuelve 201 con Location y la zona por defecto")]
    public async Task Alta_responde_201_con_location_y_zona_por_defecto()
    {
        var respuesta = await AdminDeA().PostAsJsonAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        var id = raiz.GetProperty("id").GetGuid();
        Assert.Equal("America/Mexico_City", raiz.GetProperty("zonaHoraria").GetString());
        Assert.Equal("+525512345678", raiz.GetProperty("telefono").GetString());
        Assert.Equal("centro", raiz.GetProperty("slug").GetString());
        Assert.True(raiz.GetProperty("activa").GetBoolean());
        Assert.Equal(JsonValueKind.Null, raiz.GetProperty("phoneNumberId").ValueKind);
        Assert.Equal($"{Ruta}/{id}", respuesta.Headers.Location?.ToString());
        var guardada = Assert.Single(_fabrica.Sucursales.Sucursales);
        Assert.Equal(_a.Id, guardada.RestauranteId);
        Assert.Equal(7, _fabrica.Sucursales.Horarios.Count);
    }

    [Fact(DisplayName = "003/RF-02: un phoneNumberId vacío se trata como ausente")]
    public async Task PhoneNumberIdVacio_se_guarda_como_nulo()
    {
        var respuesta = await AdminDeA().PostAsJsonAsync(Ruta, Cuerpo(phoneNumberId: ""));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Null(Assert.Single(_fabrica.Sucursales.Sucursales).PhoneNumberId);
    }

    [Theory(DisplayName = "003/RF-02: datos inválidos dan 400 por campo")]
    [InlineData("slug", "Centro", "El slug debe tener entre 3 y 40 caracteres: minúsculas, números y guiones")]
    [InlineData("telefono", "5512345678", "El teléfono debe ser un número mexicano: +52 y 10 dígitos")]
    [InlineData("prefijo", "a", "El prefijo debe tener de 1 a 3 letras mayúsculas")]
    [InlineData("zonaHoraria", "Europe/Madrid", "La zona horaria no es válida")]
    [InlineData("phoneNumberId", "x1", "El phone_number_id debe tener sólo dígitos (hasta 30)")]
    [InlineData("whatsApp", "123", "El WhatsApp debe ser un número mexicano: +52 y 10 dígitos")]
    [InlineData("nombre", " ", "El nombre es obligatorio")]
    [InlineData("direccion", "", "La dirección es obligatoria")]
    public async Task DatosInvalidos_dan_400_por_campo(string campo, string valor, string mensaje)
    {
        var cuerpo = Cuerpo();
        cuerpo[campo] = valor;

        var respuesta = await AdminDeA().PostAsJsonAsync(Ruta, cuerpo);

        await AssertValidacionAsync(respuesta, campo, mensaje);
        Assert.Empty(_fabrica.Sucursales.Sucursales);
    }

    [Fact(DisplayName = "003/RF-02: las claves de error salen en el nivel superior")]
    public async Task ClavesDeErrorSinPrefijo()
    {
        var respuesta = await AdminDeA().PostAsJsonAsync(Ruta, new Dictionary<string, object?>
        {
            ["nombre"] = "",
            ["direccion"] = "",
            ["telefono"] = "1",
            ["slug"] = "X",
            ["whatsApp"] = "2",
            ["phoneNumberId"] = "x",
            ["prefijo"] = "z",
            ["zonaHoraria"] = "Nada/Nada",
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var claves = json.RootElement.GetProperty("errors").EnumerateObject().Select(p => p.Name).OrderBy(c => c).ToArray();
        Assert.Equal(
            new[] { "direccion", "nombre", "phoneNumberId", "prefijo", "slug", "telefono", "whatsApp", "zonaHoraria" },
            claves);
    }

    [Fact(DisplayName = "003/RF-03: nombre, slug o prefijo repetidos en el restaurante dan 409")]
    public async Task Repetidos_en_el_restaurante_dan_409()
    {
        Sembrar(_a.Id, "centro", "Centro", "+525500000001", "C");
        var cliente = AdminDeA();

        var porNombre = await cliente.PostAsJsonAsync(Ruta,
            Cuerpo(nombre: "Centro", slug: "otra", whatsApp: "+525500000002", prefijo: "D"));
        var porSlug = await cliente.PostAsJsonAsync(Ruta,
            Cuerpo(nombre: "Otra", slug: "centro", whatsApp: "+525500000002", prefijo: "D"));
        var porPrefijo = await cliente.PostAsJsonAsync(Ruta,
            Cuerpo(nombre: "Otra", slug: "otra", whatsApp: "+525500000002", prefijo: "C"));

        Assert.Equal(HttpStatusCode.Conflict, porNombre.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, porSlug.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, porPrefijo.StatusCode);
        Assert.Single(_fabrica.Sucursales.Sucursales);
    }

    [Fact(DisplayName = "003/RF-03: un WhatsApp de otra sucursal de cualquier restaurante da 409")]
    public async Task WhatsApp_de_otro_restaurante_da_409()
    {
        Sembrar(_b.Id, "ajena", "Ajena", "+525512345679", "A");

        var respuesta = await AdminDeA().PostAsJsonAsync(Ruta, Cuerpo(whatsApp: "+52 55 1234 5679"));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("Ya existe una sucursal con ese WhatsApp", json.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "003/RF-03: el mismo prefijo en otro restaurante se acepta")]
    public async Task Mismo_prefijo_en_otro_restaurante_se_acepta()
    {
        Sembrar(_b.Id, "ajena", "Centro", "+525500000001", "C");

        var respuesta = await AdminDeA().PostAsJsonAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Theory(DisplayName = "003/RF-07: un rol distinto de administrador_general recibe 403")]
    [InlineData(Rol.Cajero)]
    [InlineData(Rol.Cocinero)]
    [InlineData(Rol.AdministradorSucursal)]
    public async Task Otro_rol_recibe_403(Rol rol)
    {
        var respuesta = await ClienteComo(rol, _a.Id).PostAsJsonAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Empty(_fabrica.Sucursales.Sucursales);
    }

    private Sucursal SembrarYObtener(Guid restauranteId, string slug, string nombre, string whatsApp, string prefijo)
    {
        Sembrar(restauranteId, slug, nombre, whatsApp, prefijo);
        return _fabrica.Sucursales.Sucursales[^1];
    }

    [Fact(DisplayName = "003/RF-04: GET lista nombre, dirección, slug, prefijo y estado de las sucursales del restaurante")]
    public async Task Listar_devuelve_solo_las_del_restaurante()
    {
        Sembrar(_a.Id, "norte", "Norte", "+525500000002", "N");
        Sembrar(_a.Id, "centro", "Centro", "+525500000001", "C");
        Sembrar(_b.Id, "ajena", "Ajena", "+525500000005", "A");

        var respuesta = await AdminDeA().GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var filas = json.RootElement.EnumerateArray().ToList();
        Assert.Equal(new[] { "Centro", "Norte" }, filas.Select(f => f.GetProperty("nombre").GetString()));
        var primera = filas[0];
        Assert.Equal("Calle 1", primera.GetProperty("direccion").GetString());
        Assert.Equal("centro", primera.GetProperty("slug").GetString());
        Assert.Equal("C", primera.GetProperty("prefijo").GetString());
        Assert.True(primera.GetProperty("activa").GetBoolean());
    }

    [Fact(DisplayName = "003/RF-05: editar cambia los datos y no el slug")]
    public async Task Editar_cambia_datos_y_no_el_slug()
    {
        var existente = SembrarYObtener(_a.Id, "centro", "Centro", "+525500000001", "C");
        var cuerpo = Cuerpo(nombre: "Centro Nuevo", slug: "otro", prefijo: "N", zonaHoraria: "America/Tijuana");

        var respuesta = await AdminDeA().PutAsJsonAsync($"{Ruta}/{existente.Id}", cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("centro", json.RootElement.GetProperty("slug").GetString());
        Assert.Equal("Centro Nuevo", json.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("+525512345678", json.RootElement.GetProperty("telefono").GetString());
        Assert.Equal("centro", existente.Slug);
        Assert.Equal("Centro Nuevo", existente.Nombre);
        Assert.Equal("America/Tijuana", existente.ZonaHoraria);
    }

    [Fact(DisplayName = "003/RF-05: editar con datos inválidos da 400 por campo")]
    public async Task Editar_con_datos_invalidos_da_400()
    {
        var existente = SembrarYObtener(_a.Id, "centro", "Centro", "+525500000001", "C");

        var respuesta = await AdminDeA().PutAsJsonAsync($"{Ruta}/{existente.Id}", Cuerpo(prefijo: "a"));

        await AssertValidacionAsync(respuesta, "prefijo", "El prefijo debe tener de 1 a 3 letras mayúsculas");
        Assert.Equal("C", existente.Prefijo);
    }

    [Fact(DisplayName = "003/RF-05: editar con un prefijo de otra sucursal da 409 y con el propio no")]
    public async Task Editar_prefijo_ajeno_da_409_y_propio_no()
    {
        var existente = SembrarYObtener(_a.Id, "centro", "Centro", "+525500000001", "C");
        Sembrar(_a.Id, "norte", "Norte", "+525500000002", "N");
        var cliente = AdminDeA();

        var ajeno = await cliente.PutAsJsonAsync($"{Ruta}/{existente.Id}", Cuerpo(nombre: "Centro", prefijo: "N", whatsApp: "+525500000001"));
        var propio = await cliente.PutAsJsonAsync($"{Ruta}/{existente.Id}", Cuerpo(nombre: "Centro", prefijo: "C", whatsApp: "+525500000001"));

        Assert.Equal(HttpStatusCode.Conflict, ajeno.StatusCode);
        Assert.Equal(HttpStatusCode.OK, propio.StatusCode);
    }

    [Fact(DisplayName = "003/RF-05: editar una sucursal de otro restaurante da 404")]
    public async Task Editar_de_otro_restaurante_da_404()
    {
        var ajena = SembrarYObtener(_b.Id, "ajena", "Ajena", "+525500000005", "A");

        var respuesta = await AdminDeA().PutAsJsonAsync($"{Ruta}/{ajena.Id}", Cuerpo());
        var inexistente = await AdminDeA().PutAsJsonAsync($"{Ruta}/{Guid.NewGuid()}", Cuerpo());

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal("Ajena", ajena.Nombre);
    }

    [Fact(DisplayName = "003/RF-06: desactivar y reactivar conserva el horario (el almacén de cierres llega en T-09)")]
    public async Task Desactivar_y_reactivar_conserva_horario()
    {
        var existente = SembrarYObtener(_a.Id, "centro", "Centro", "+525500000001", "C");
        var ajena = SembrarYObtener(_b.Id, "ajena", "Ajena", "+525500000005", "A");
        var horarioPropio = HorarioDia.Crear(_a.Id, existente.Id, DiaSemana.Lunes, new TimeOnly(9, 0), new TimeOnly(18, 0));
        var horarioAjeno = HorarioDia.Crear(_b.Id, ajena.Id, DiaSemana.Martes, new TimeOnly(10, 0), new TimeOnly(20, 0));
        _fabrica.Sucursales.Horarios.AddRange([horarioPropio, horarioAjeno]);
        var cliente = AdminDeA();

        var apagada = await cliente.PutAsJsonAsync($"{Ruta}/{existente.Id}/estado", new { activa = false });
        Assert.Equal(HttpStatusCode.OK, apagada.StatusCode);
        using (var json = JsonDocument.Parse(await apagada.Content.ReadAsStringAsync()))
        {
            Assert.False(json.RootElement.GetProperty("activa").GetBoolean());
        }

        Assert.False(existente.Activa);
        Assert.True(ajena.Activa);

        var encendida = await cliente.PutAsJsonAsync($"{Ruta}/{existente.Id}/estado", new { activa = true });
        Assert.Equal(HttpStatusCode.OK, encendida.StatusCode);
        Assert.True(existente.Activa);

        Assert.Equal(new[] { horarioPropio, horarioAjeno }, _fabrica.Sucursales.Horarios);
        Assert.Equal(new TimeOnly(9, 0), horarioPropio.Apertura);
        Assert.Equal(new TimeOnly(18, 0), horarioPropio.Cierre);
        Assert.Equal(new TimeOnly(10, 0), horarioAjeno.Apertura);
    }

    [Fact(DisplayName = "003/RF-06: cambiar estado sin activa da 400 y de otro restaurante da 404")]
    public async Task Estado_sin_activa_da_400_y_ajena_da_404()
    {
        var existente = SembrarYObtener(_a.Id, "centro", "Centro", "+525500000001", "C");
        var ajena = SembrarYObtener(_b.Id, "ajena", "Ajena", "+525500000005", "A");
        var cliente = AdminDeA();

        var sinCampo = await cliente.PutAsJsonAsync($"{Ruta}/{existente.Id}/estado", new Dictionary<string, object?>());
        var deOtro = await cliente.PutAsJsonAsync($"{Ruta}/{ajena.Id}/estado", new { activa = false });

        await AssertValidacionAsync(sinCampo, "activa", "El estado es obligatorio");
        Assert.Equal(HttpStatusCode.NotFound, deOtro.StatusCode);
        Assert.True(ajena.Activa);
    }

    [Theory(DisplayName = "003/RF-07: listar, editar y cambiar estado exigen administrador_general")]
    [InlineData(Rol.Cajero)]
    [InlineData(Rol.AdministradorSucursal)]
    public async Task Listar_editar_y_estado_exigen_administrador_general(Rol rol)
    {
        var existente = SembrarYObtener(_a.Id, "centro", "Centro", "+525500000001", "C");
        var cliente = ClienteComo(rol, _a.Id);

        var listar = await cliente.GetAsync(Ruta);
        var editar = await cliente.PutAsJsonAsync($"{Ruta}/{existente.Id}", Cuerpo(nombre: "Cambiado"));
        var estado = await cliente.PutAsJsonAsync($"{Ruta}/{existente.Id}/estado", new { activa = false });

        Assert.Equal(HttpStatusCode.Forbidden, listar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, editar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, estado.StatusCode);
        Assert.Equal("Centro", existente.Nombre);
        Assert.True(existente.Activa);
    }

    [Fact(DisplayName = "003/RF-07: un token de plataforma recibe 403")]
    public async Task Token_de_plataforma_recibe_403()
    {
        var respuesta = await ClienteComo(Rol.AdministradorPlataforma, null).PostAsJsonAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Empty(_fabrica.Sucursales.Sucursales);
    }
}
