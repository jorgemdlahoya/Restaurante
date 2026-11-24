using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Auth;

[Collection(ColeccionApi.Nombre)]
public sealed class LoginPorRestauranteTests : IDisposable
{
    private const string Contrasena = "Clave-de-prueba-123";
    private const string MensajeCredenciales = "Usuario o contraseña incorrectos";
    private const string RutaPlataforma = "/api/plataforma/auth/login";

    private readonly FabricaApi _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    private Usuario Sembrar(Guid? restauranteId, string nombre, Rol rol)
    {
        var hasher = _fabrica.Services.GetRequiredService<IHasherContrasenas>();
        var usuario = Usuario.Crear(restauranteId, nombre, hasher.Hashear(Contrasena), rol, DateTimeOffset.UtcNow);
        _fabrica.Usuarios.Agregar(usuario);
        return usuario;
    }

    private static string RutaRestaurante(string slug) => $"/api/r/{slug}/auth/login";

    private static Task<HttpResponseMessage> LoginAsync(HttpClient cliente, string ruta, string nombre, string contrasena = Contrasena) =>
        cliente.PostAsJsonAsync(ruta, new { nombreUsuario = nombre, contrasena });

    private static JwtSecurityToken LeerToken(string token) =>
        new JwtSecurityTokenHandler { MapInboundClaims = false }.ReadJwtToken(token);

    private static async Task<string> TokenDeAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("token").GetString()!;
    }

    private static async Task<(int Estado, string? Titulo, string? Detalle)> ProblemaDeAsync(HttpResponseMessage respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        return (raiz.GetProperty("status").GetInt32(), raiz.GetProperty("title").GetString(),
            raiz.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "002/RF-08: login en su restaurante devuelve JWT con sub, role y restaurant_id")]
    public async Task Login_en_su_restaurante_devuelve_jwt_con_restaurant_id()
    {
        var restaurante = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        var usuario = Sembrar(restaurante.Id, "ana", Rol.Cajero);

        var respuesta = await LoginAsync(_fabrica.CreateClient(), RutaRestaurante("la-esquina"), "ana");

        var jwt = LeerToken(await TokenDeAsync(respuesta));
        Assert.Equal(usuario.Id.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal("cajero", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal(restaurante.Id.ToString(), jwt.Claims.Single(c => c.Type == "restaurant_id").Value);
    }

    [Fact(DisplayName = "002/RF-09: usuario de otro restaurante recibe 401 con el mismo mensaje")]
    public async Task Usuario_de_otro_restaurante_recibe_401_con_el_mismo_mensaje()
    {
        var esquina = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        var otro = _fabrica.SembrarRestaurante("El Otro", "el-otro");
        Sembrar(esquina.Id, "ana", Rol.Cajero);
        Sembrar(otro.Id, "beto", Rol.Cajero);
        var cliente = _fabrica.CreateClient();

        var deOtroRestaurante = await LoginAsync(cliente, RutaRestaurante("la-esquina"), "beto");
        var contrasenaIncorrecta = await LoginAsync(cliente, RutaRestaurante("la-esquina"), "ana", "otra-cosa");

        Assert.Equal(HttpStatusCode.Unauthorized, deOtroRestaurante.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, contrasenaIncorrecta.StatusCode);
        var problema = await ProblemaDeAsync(deOtroRestaurante);
        Assert.Equal(MensajeCredenciales, problema.Detalle);
        Assert.Equal(await ProblemaDeAsync(contrasenaIncorrecta), problema);
    }

    [Theory(DisplayName = "002/RF-10: slug inexistente o restaurante inactivo dan 404 Restaurante no disponible")]
    [InlineData("no-existe")]
    [InlineData("cerrado")]
    public async Task Slug_inexistente_o_inactivo_dan_404(string slug)
    {
        var cerrado = _fabrica.SembrarRestaurante("Cerrado", "cerrado", activo: false);
        Sembrar(cerrado.Id, "ana", Rol.Cajero);

        var respuesta = await LoginAsync(_fabrica.CreateClient(), RutaRestaurante(slug), "ana");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var problema = await ProblemaDeAsync(respuesta);
        Assert.Equal(404, problema.Estado);
        Assert.Equal("Restaurante no disponible", problema.Titulo);
        Assert.Equal("La dirección no corresponde a un restaurante activo.", problema.Detalle);
    }

    [Fact(DisplayName = "002/RF-10: el slug de la ruta no distingue mayúsculas")]
    public async Task El_slug_de_la_ruta_no_distingue_mayusculas()
    {
        var restaurante = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        Sembrar(restaurante.Id, "ana", Rol.Cajero);

        var respuesta = await LoginAsync(_fabrica.CreateClient(), RutaRestaurante("La-Esquina"), "ana");

        var jwt = LeerToken(await TokenDeAsync(respuesta));
        Assert.Equal(restaurante.Id.ToString(), jwt.Claims.Single(c => c.Type == "restaurant_id").Value);
    }

    [Fact(DisplayName = "002/RF-11: login de plataforma devuelve rol administrador_plataforma sin restaurant_id")]
    public async Task Login_de_plataforma_devuelve_rol_sin_restaurant_id()
    {
        Sembrar(null, "raiz", Rol.AdministradorPlataforma);

        var respuesta = await LoginAsync(_fabrica.CreateClient(), RutaPlataforma, "raiz");

        var token = await TokenDeAsync(respuesta);
        var jwt = LeerToken(token);
        Assert.Equal("administrador_plataforma", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "restaurant_id");
    }

    [Fact(DisplayName = "002/RF-11: un usuario de restaurante no puede entrar por el login de plataforma")]
    public async Task Usuario_de_restaurante_no_entra_por_el_login_de_plataforma()
    {
        var restaurante = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        Sembrar(restaurante.Id, "ana", Rol.AdministradorGeneral);
        var cliente = _fabrica.CreateClient();

        var sinToken = await LoginAsync(cliente, RutaPlataforma, "ana");
        var tokenRestaurante = await TokenDeAsync(await LoginAsync(cliente, RutaRestaurante("la-esquina"), "ana"));
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenRestaurante);
        var conTokenDeRestaurante = await LoginAsync(cliente, RutaPlataforma, "ana");

        foreach (var respuesta in new[] { sinToken, conTokenDeRestaurante })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
            Assert.Equal(MensajeCredenciales, (await ProblemaDeAsync(respuesta)).Detalle);
        }
    }
}
