using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Seguridad;

/// <summary>Sello de sesión: el token sólo vale mientras el usuario siga activo y con el mismo sello (004/RF-12..RF-14).</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class SesionUsuarioTests : IDisposable
{
    private const string Contrasena = "Clave-de-prueba-123";
    private const string TituloSesionInvalida = "Sesión no válida";
    private const string DetalleSesionInvalida = "Vuelve a iniciar sesión.";
    private const string RutaUsuarios = "/api/r/la-esquina/usuarios";
    private const string RutaPlataforma = "/api/plataforma/restaurantes";

    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _restaurante;

    public SesionUsuarioTests() => _restaurante = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");

    public void Dispose() => _fabrica.Dispose();

    private Usuario Sembrar(string nombre, Rol rol, IReadOnlyCollection<Guid>? sucursales = null)
    {
        var hasher = _fabrica.Services.GetRequiredService<IHasherContrasenas>();
        var restauranteId = rol == Rol.AdministradorPlataforma ? (Guid?)null : _restaurante.Id;
        var usuario = Usuario.Crear(
            restauranteId, nombre, hasher.Hashear(Contrasena), rol, sucursales ?? [], DateTimeOffset.UtcNow);
        _fabrica.Usuarios.Agregar(usuario);
        return usuario;
    }

    /// <summary>Token emitido con el emisor real de la API, como el que devuelve el login.</summary>
    private string TokenDe(Usuario usuario) =>
        _fabrica.Services.GetRequiredService<IEmisorTokens>().Emitir(usuario).Token;

    private Task<HttpResponseMessage> GetAsync(string ruta, string token)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente.GetAsync(ruta);
    }

    private static async Task AssertSesionInvalidaAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(TituloSesionInvalida, json.RootElement.GetProperty("title").GetString());
        Assert.Equal(DetalleSesionInvalida, json.RootElement.GetProperty("detail").GetString());
        Assert.Equal(401, json.RootElement.GetProperty("status").GetInt32());
    }

    [Fact(DisplayName = "004/RF-12: un usuario desactivado no inicia sesión")]
    public async Task Usuario_desactivado_no_inicia_sesion()
    {
        Sembrar("baja", Rol.Cajero, [Guid.NewGuid()]).Desactivar();

        var respuesta = await _fabrica.CreateClient().PostAsJsonAsync(
            "/api/r/la-esquina/auth/login", new { nombreUsuario = "baja", contrasena = Contrasena });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("Usuario o contraseña incorrectos", json.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "004/RF-13: tras desactivar, el token anterior responde 401 Sesión no válida")]
    public async Task Tras_desactivar_el_token_anterior_responde_401()
    {
        var admin = Sembrar("jefa", Rol.AdministradorGeneral);
        var token = TokenDe(admin);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync(RutaUsuarios, token)).StatusCode);

        admin.Desactivar();

        await AssertSesionInvalidaAsync(await GetAsync(RutaUsuarios, token));
    }

    [Fact(DisplayName = "004/RF-13: tras cambiar el sello, un token nuevo funciona")]
    public async Task Tras_cambiar_el_sello_un_token_nuevo_funciona()
    {
        var admin = Sembrar("jefa", Rol.AdministradorGeneral);
        var anterior = TokenDe(admin);

        admin.CambiarContrasena("otro-hash");
        var nuevo = TokenDe(admin);

        await AssertSesionInvalidaAsync(await GetAsync(RutaUsuarios, anterior));
        Assert.Equal(HttpStatusCode.OK, (await GetAsync(RutaUsuarios, nuevo)).StatusCode);
    }

    [Fact(DisplayName = "004/RF-13: el usuario actual lleva el rol y las sucursales de la base de datos")]
    public async Task Usuario_actual_lleva_los_datos_de_la_base()
    {
        var centro = Guid.NewGuid();
        var norte = Guid.NewGuid();
        var cajero = Sembrar("caja", Rol.Cajero, [centro, norte]);

        var respuesta = await GetAsync("/prueba/sesion", TokenDe(cajero));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(cajero.Id, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("cajero", json.RootElement.GetProperty("rol").GetString());
        var sucursales = json.RootElement.GetProperty("sucursales").EnumerateArray().Select(e => e.GetGuid()).ToHashSet();
        Assert.True(sucursales.SetEquals([centro, norte]));
    }

    [Fact(DisplayName = "004/RF-14: un token sin sello responde 401 Sesión no válida")]
    public async Task Token_sin_sello_responde_401()
    {
        var admin = Sembrar("jefa", Rol.AdministradorGeneral);

        // Token emitido antes de la migración: firma válida, sin claim "sello" (Foco 3: nunca 500).
        var sinSello = FabricaApi.FirmarToken(Rol.AdministradorGeneral, admin.Id, sello: null,
            restauranteId: _restaurante.Id);
        var selloIlegible = FabricaApi.FirmarToken(Rol.AdministradorGeneral, admin.Id, sello: "no-es-un-guid",
            restauranteId: _restaurante.Id);

        await AssertSesionInvalidaAsync(await GetAsync(RutaUsuarios, sinSello));
        await AssertSesionInvalidaAsync(await GetAsync(RutaUsuarios, selloIlegible));
    }

    [Fact(DisplayName = "004/RF-14: un sello que no coincide responde 401 Sesión no válida")]
    public async Task Sello_distinto_responde_401()
    {
        var admin = Sembrar("jefa", Rol.AdministradorGeneral);
        var token = FabricaApi.FirmarToken(Rol.AdministradorGeneral, admin.Id, Guid.NewGuid().ToString("D"),
            restauranteId: _restaurante.Id);

        await AssertSesionInvalidaAsync(await GetAsync(RutaUsuarios, token));
    }

    [Fact(DisplayName = "004/RF-14: un usuario inactivo con el sello vigente responde 401 Sesión no válida")]
    public async Task Usuario_inactivo_con_sello_vigente_responde_401()
    {
        var admin = Sembrar("jefa", Rol.AdministradorGeneral);
        admin.Desactivar();

        // Token con el sello ACTUAL (ya renovado al desactivar): sólo la comprobación de Activo puede rechazarlo.
        var token = FabricaApi.FirmarToken(Rol.AdministradorGeneral, admin.Id, admin.SelloSesion.ToString("D"),
            restauranteId: _restaurante.Id);

        await AssertSesionInvalidaAsync(await GetAsync(RutaUsuarios, token));
    }

    [Fact(DisplayName = "004/RF-14: un token de un usuario inexistente responde 401")]
    public async Task Token_de_usuario_inexistente_responde_401()
    {
        var token = FabricaApi.FirmarToken(Rol.AdministradorGeneral, Guid.NewGuid(), Guid.NewGuid().ToString("D"),
            restauranteId: _restaurante.Id);

        await AssertSesionInvalidaAsync(await GetAsync(RutaUsuarios, token));
    }

    [Fact(DisplayName = "004/RF-14: también en /api/plataforma")]
    public async Task Tambien_en_plataforma()
    {
        var raiz = Sembrar("raiz", Rol.AdministradorPlataforma);
        var token = TokenDe(raiz);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync(RutaPlataforma, token)).StatusCode);

        raiz.CambiarContrasena("otro-hash");

        await AssertSesionInvalidaAsync(await GetAsync(RutaPlataforma, token));
        var inexistente = FabricaApi.FirmarToken(Rol.AdministradorPlataforma, Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        await AssertSesionInvalidaAsync(await GetAsync(RutaPlataforma, inexistente));
    }

    [Fact(DisplayName = "004/RF-14: el 401 anuncia WWW-Authenticate Bearer")]
    public async Task El_401_anuncia_www_authenticate_bearer()
    {
        var token = FabricaApi.FirmarToken(Rol.AdministradorGeneral, Guid.NewGuid(), sello: null,
            restauranteId: _restaurante.Id);

        var respuesta = await GetAsync(RutaUsuarios, token);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("Bearer", Assert.Single(respuesta.Headers.WwwAuthenticate).Scheme);
    }
}
