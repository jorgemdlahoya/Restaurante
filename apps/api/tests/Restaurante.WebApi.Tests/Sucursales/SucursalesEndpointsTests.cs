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

    [Fact(DisplayName = "003/RF-07: un token de plataforma recibe 403")]
    public async Task Token_de_plataforma_recibe_403()
    {
        var respuesta = await ClienteComo(Rol.AdministradorPlataforma, null).PostAsJsonAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Empty(_fabrica.Sucursales.Sucursales);
    }
}
