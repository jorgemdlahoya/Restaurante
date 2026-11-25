using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using static Restaurante.WebApi.Tests.Infraestructura.AfirmacionesHttp;

namespace Restaurante.WebApi.Tests.Restaurantes;

[Collection(ColeccionApi.Nombre)]
public sealed class RestaurantesEndpointsTests : IDisposable
{
    private const string Ruta = "/api/plataforma/restaurantes";

    private readonly FabricaApi _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    private HttpClient ClienteComo(Rol rol)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fabrica.EmitirToken(rol));
        return cliente;
    }

    private static object Cuerpo(
        string nombre = "La Esquina", string slug = "la-esquina", object? paleta = null,
        string usuario = "admin.esquina", string contrasena = "secreta123") =>
        new { nombre, slug, paleta, administrador = new { nombreUsuario = usuario, contrasena } };

    [Fact(DisplayName = "002/RF-01: POST devuelve 201 con Location y el restaurante")]
    public async Task Post_devuelve_201_con_location_y_restaurante()
    {
        var respuesta = await ClienteComo(Rol.AdministradorPlataforma).PostAsJsonAsync(Ruta,
            Cuerpo(paleta: new { principal = "#112233" }));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        var id = raiz.GetProperty("id").GetGuid();
        Assert.EndsWith($"{Ruta}/{id}", respuesta.Headers.Location?.ToString());
        Assert.True(raiz.GetProperty("activo").GetBoolean());
        Assert.Equal("la-esquina", raiz.GetProperty("slug").GetString());
        Assert.Equal("#112233", raiz.GetProperty("paleta").GetProperty("principal").GetString());
        var admin = Assert.Single(_fabrica.Usuarios.Usuarios, u => u.NombreUsuario == "admin.esquina");
        Assert.Equal(Rol.AdministradorGeneral, admin.Rol);
        Assert.Equal(id, admin.RestauranteId);
    }

    [Theory(DisplayName = "002/RF-02: datos inválidos dan 400 por campo")]
    [InlineData("slug", "El slug debe tener entre 3 y 40 caracteres: minúsculas, números y guiones")]
    [InlineData("nombre", "El nombre es obligatorio")]
    [InlineData("paleta.principal", "El color debe tener el formato #RRGGBB")]
    [InlineData("administrador.contrasena", "La contraseña debe tener al menos 8 caracteres")]
    public async Task Datos_invalidos_dan_400_por_campo(string campo, string mensaje)
    {
        object cuerpo = campo switch
        {
            "slug" => Cuerpo(slug: "ab"),
            "nombre" => Cuerpo(nombre: ""),
            "paleta.principal" => Cuerpo(paleta: new { principal = "rojo" }),
            _ => Cuerpo(contrasena: "corta"),
        };

        var respuesta = await ClienteComo(Rol.AdministradorPlataforma).PostAsJsonAsync(Ruta, cuerpo);

        await AssertValidacionAsync(respuesta, campo, mensaje);
        Assert.Empty(_fabrica.Restaurantes.Restaurantes);
    }

    [Fact(DisplayName = "002/RF-02: un slug con salto de línea final da 400 y no 500")]
    public async Task Slug_con_salto_de_linea_da_400()
    {
        var respuesta = await ClienteComo(Rol.AdministradorPlataforma).PostAsJsonAsync(Ruta, Cuerpo(slug: "abc\n"));

        await AssertValidacionAsync(respuesta, "slug",
            "El slug debe tener entre 3 y 40 caracteres: minúsculas, números y guiones");
    }

    [Fact(DisplayName = "002/RF-02: un cuerpo sin administrador ni nombre da 400 por campo")]
    public async Task Cuerpo_vacio_da_400()
    {
        var respuesta = await ClienteComo(Rol.AdministradorPlataforma).PostAsJsonAsync(Ruta, new { });

        await AssertValidacionAsync(respuesta, "administrador.nombreUsuario", "El nombre de usuario es obligatorio");
    }

    [Fact(DisplayName = "002/RF-02: nombre o slug repetidos dan 409")]
    public async Task Nombre_o_slug_repetidos_dan_409()
    {
        var cliente = ClienteComo(Rol.AdministradorPlataforma);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync(Ruta, Cuerpo())).StatusCode);

        var porNombre = await cliente.PostAsJsonAsync(Ruta, Cuerpo(slug: "otro-slug"));
        var porSlug = await cliente.PostAsJsonAsync(Ruta, Cuerpo(nombre: "Otro nombre"));

        Assert.Equal(HttpStatusCode.Conflict, porNombre.StatusCode);
        Assert.Contains("Ya existe un restaurante con ese nombre", await porNombre.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict, porSlug.StatusCode);
        Assert.Contains("Ya existe un restaurante con ese slug", await porSlug.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "002/RF-02: el nombre se recorta y '  La Esquina ' duplica 'La Esquina'")]
    public async Task Nombre_con_espacios_duplica()
    {
        var cliente = ClienteComo(Rol.AdministradorPlataforma);
        await cliente.PostAsJsonAsync(Ruta, Cuerpo());

        var respuesta = await cliente.PostAsJsonAsync(Ruta, Cuerpo(nombre: "  La Esquina ", slug: "otra-esquina"));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Theory(DisplayName = "002/RF-07: un rol distinto de administrador_plataforma recibe 403")]
    [InlineData(Rol.AdministradorGeneral)]
    [InlineData(Rol.AdministradorSucursal)]
    [InlineData(Rol.Cajero)]
    [InlineData(Rol.Cocinero)]
    public async Task Rol_de_restaurante_recibe_403(Rol rol)
    {
        var respuesta = await ClienteComo(rol).PostAsJsonAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Empty(_fabrica.Restaurantes.Restaurantes);
    }

    [Fact(DisplayName = "002/RF-07: sin token recibe 401")]
    public async Task Sin_token_recibe_401()
    {
        var respuesta = await _fabrica.CreateClient().PostAsJsonAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private Guid Sembrar(string nombre, string slug, Restaurante.Domain.Restaurantes.Paleta? paleta = null)
    {
        var restaurante = Restaurante.Domain.Restaurantes.Restaurante.Crear(
            nombre, slug, paleta ?? Restaurante.Domain.Restaurantes.Paleta.Vacia, DateTimeOffset.UtcNow);
        _fabrica.Restaurantes.Agregar(restaurante);
        return restaurante.Id;
    }

    [Fact(DisplayName = "002/RF-03: GET lista nombre, slug, estado y paleta de cada restaurante")]
    public async Task Get_lista_restaurantes()
    {
        Sembrar("Alfa", "alfa", new Restaurante.Domain.Restaurantes.Paleta("#112233", null, null, null, null));
        Sembrar("Beta", "beta");

        var respuesta = await ClienteComo(Rol.AdministradorPlataforma).GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var arreglo = json.RootElement.EnumerateArray().ToList();
        Assert.Equal(2, arreglo.Count);
        Assert.Equal("Alfa", arreglo[0].GetProperty("nombre").GetString());
        Assert.Equal("alfa", arreglo[0].GetProperty("slug").GetString());
        Assert.True(arreglo[0].GetProperty("activo").GetBoolean());
        Assert.Equal("#112233", arreglo[0].GetProperty("paleta").GetProperty("principal").GetString());
        Assert.Equal(JsonValueKind.Null, arreglo[1].GetProperty("paleta").GetProperty("principal").ValueKind);
    }

    [Fact(DisplayName = "002/RF-04: editar cambia nombre y paleta y no el slug")]
    public async Task Put_edita_sin_cambiar_slug()
    {
        var id = Sembrar("Viejo", "viejo");

        var respuesta = await ClienteComo(Rol.AdministradorPlataforma).PutAsJsonAsync($"{Ruta}/{id}",
            new { nombre = "Nuevo", paleta = new { principal = "#ABCDEF" }, slug = "otro" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("Nuevo", json.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("viejo", json.RootElement.GetProperty("slug").GetString());
        Assert.Equal("#ABCDEF", json.RootElement.GetProperty("paleta").GetProperty("principal").GetString());
        var guardado = Assert.Single(_fabrica.Restaurantes.Restaurantes);
        Assert.Equal("viejo", guardado.Slug);
        Assert.Equal("Nuevo", guardado.Nombre);
    }

    [Fact(DisplayName = "002/RF-04: editar un id inexistente da 404 y un nombre ajeno da 409")]
    public async Task Put_inexistente_404_y_nombre_ajeno_409()
    {
        var cliente = ClienteComo(Rol.AdministradorPlataforma);
        var id = Sembrar("Propio", "propio");
        Sembrar("Ajeno", "ajeno");

        var inexistente = await cliente.PutAsJsonAsync($"{Ruta}/{Guid.NewGuid()}", new { nombre = "X" });
        var ajeno = await cliente.PutAsJsonAsync($"{Ruta}/{id}", new { nombre = " Ajeno " });
        var sinNombre = await cliente.PutAsJsonAsync($"{Ruta}/{id}", new { });

        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, ajeno.StatusCode);
        await AssertValidacionAsync(sinNombre, "nombre", "El nombre es obligatorio");
    }

    [Fact(DisplayName = "002/RF-06: PUT estado desactiva y reactiva sin perder datos")]
    public async Task Put_estado_desactiva_y_reactiva()
    {
        var id = Sembrar("Resto", "resto", new Restaurante.Domain.Restaurantes.Paleta("#112233", null, null, null, null));
        var cliente = ClienteComo(Rol.AdministradorPlataforma);

        var desactivar = await cliente.PutAsJsonAsync($"{Ruta}/{id}/estado", new { activo = false });
        using var inactivo = JsonDocument.Parse(await desactivar.Content.ReadAsStringAsync());
        var reactivar = await cliente.PutAsJsonAsync($"{Ruta}/{id}/estado", new { activo = true });
        using var activo = JsonDocument.Parse(await reactivar.Content.ReadAsStringAsync());

        Assert.False(inactivo.RootElement.GetProperty("activo").GetBoolean());
        var raiz = activo.RootElement;
        Assert.True(raiz.GetProperty("activo").GetBoolean());
        Assert.Equal("Resto", raiz.GetProperty("nombre").GetString());
        Assert.Equal("resto", raiz.GetProperty("slug").GetString());
        Assert.Equal("#112233", raiz.GetProperty("paleta").GetProperty("principal").GetString());
    }

    [Fact(DisplayName = "002/RF-06: PUT estado sin activo da 400 y con id inexistente 404")]
    public async Task Put_estado_validacion_y_404()
    {
        var id = Sembrar("Resto", "resto");
        var cliente = ClienteComo(Rol.AdministradorPlataforma);

        var sin = await cliente.PutAsJsonAsync($"{Ruta}/{id}/estado", new { });
        var inexistente = await cliente.PutAsJsonAsync($"{Ruta}/{Guid.NewGuid()}/estado", new { activo = false });

        await AssertValidacionAsync(sin, "activo", "El estado es obligatorio");
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }

    [Fact(DisplayName = "002/RF-07: listar, editar y cambiar estado exigen administrador_plataforma")]
    public async Task Listar_editar_y_estado_exigen_plataforma()
    {
        var id = Sembrar("Resto", "resto");
        var cliente = ClienteComo(Rol.AdministradorGeneral);

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(Ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PutAsJsonAsync($"{Ruta}/{id}", new { nombre = "Otro" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PutAsJsonAsync($"{Ruta}/{id}/estado", new { activo = false })).StatusCode);
        var intacto = Assert.Single(_fabrica.Restaurantes.Restaurantes);
        Assert.Equal("Resto", intacto.Nombre);
        Assert.True(intacto.Activo);
    }
}
