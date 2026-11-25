using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Auth;

/// <summary>Mi sesión y login sin sucursales activas (004/RF-16, RF-17).</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class MiSesionTests : IDisposable
{
    private const string Contrasena = "Clave-de-prueba-123";

    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _restaurante;
    private readonly Sucursal _centro;
    private readonly Sucursal _norte;
    private readonly Sucursal _sur;

    public MiSesionTests()
    {
        _restaurante = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        _centro = _fabrica.SembrarSucursal(_restaurante.Id, "centro", "Centro", "C");
        _norte = _fabrica.SembrarSucursal(_restaurante.Id, "norte", "Norte", "N");
        _sur = _fabrica.SembrarSucursal(_restaurante.Id, "sur", "Sur", "S");
    }

    public void Dispose() => _fabrica.Dispose();

    private Usuario Sembrar(string nombre, Rol rol, params Sucursal[] sucursales)
    {
        var hasher = _fabrica.Services.GetRequiredService<IHasherContrasenas>();
        var usuario = Usuario.Crear(
            _restaurante.Id, nombre, hasher.Hashear(Contrasena), rol, sucursales.Select(s => s.Id).ToList(),
            DateTimeOffset.UtcNow);
        _fabrica.Usuarios.Agregar(usuario);
        return usuario;
    }

    private HttpClient ClienteDe(Usuario usuario)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", _fabrica.EmitirToken(usuario.Rol, usuarioId: usuario.Id, restauranteId: _restaurante.Id));
        return cliente;
    }

    private static async Task<JsonElement> JsonDeAsync(HttpResponseMessage respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    [Fact(DisplayName = "004/RF-16: mi-sesion del cajero trae sólo sus sucursales activas ordenadas por nombre")]
    public async Task Cajero_trae_sus_sucursales_activas_ordenadas()
    {
        _norte.Desactivar();
        var cajero = Sembrar("caja", Rol.Cajero, _sur, _norte, _centro);

        var respuesta = await ClienteDe(cajero).GetAsync("/api/r/la-esquina/mi-sesion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var raiz = await JsonDeAsync(respuesta);
        Assert.Equal("caja", raiz.GetProperty("nombreUsuario").GetString());
        Assert.Equal("cajero", raiz.GetProperty("rol").GetString());
        var sucursales = raiz.GetProperty("sucursales").EnumerateArray().ToList();
        Assert.Equal(["centro", "sur"], sucursales.Select(s => s.GetProperty("slug").GetString()));
        Assert.Equal(["Centro", "Sur"], sucursales.Select(s => s.GetProperty("nombre").GetString()));
    }

    [Fact(DisplayName = "004/RF-16: mi-sesion del administrador general trae todas las sucursales activas")]
    public async Task General_trae_todas_las_sucursales_activas()
    {
        _sur.Desactivar();
        var general = Sembrar("dueno", Rol.AdministradorGeneral);

        var respuesta = await ClienteDe(general).GetAsync("/api/r/la-esquina/mi-sesion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var raiz = await JsonDeAsync(respuesta);
        Assert.Equal(
            ["centro", "norte"],
            raiz.GetProperty("sucursales").EnumerateArray().Select(s => s.GetProperty("slug").GetString()));
    }

    [Fact(DisplayName = "004/RF-16: mi-sesion exige sesión")]
    public async Task Sin_token_responde_401()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/api/r/la-esquina/mi-sesion");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private Task<HttpResponseMessage> LoginAsync(string nombre, string contrasena = Contrasena) =>
        _fabrica.CreateClient().PostAsJsonAsync("/api/r/la-esquina/auth/login", new { nombreUsuario = nombre, contrasena });

    [Fact(DisplayName = "004/RF-17: un cajero con todas sus sucursales inactivas recibe 403 Sin sucursales activas y ningún token")]
    public async Task Cajero_sin_sucursales_activas_recibe_403_sin_token()
    {
        Sembrar("caja", Rol.Cajero, _centro, _norte);
        _centro.Desactivar();
        _norte.Desactivar();

        var respuesta = await LoginAsync("caja");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        var raiz = await JsonDeAsync(respuesta);
        Assert.Equal("Sin sucursales activas", raiz.GetProperty("title").GetString());
        Assert.Equal(403, raiz.GetProperty("status").GetInt32());
        Assert.False(raiz.TryGetProperty("token", out _));
    }

    [Fact(DisplayName = "004/RF-17: con contraseña incorrecta sigue respondiendo 401")]
    public async Task Contrasena_incorrecta_sigue_respondiendo_401()
    {
        Sembrar("caja", Rol.Cajero, _centro);
        _centro.Desactivar();

        var respuesta = await LoginAsync("caja", "otra-clave");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact(DisplayName = "004/RF-17: un cajero con una sucursal activa inicia sesión")]
    public async Task Cajero_con_una_sucursal_activa_inicia_sesion()
    {
        Sembrar("caja", Rol.Cajero, _centro, _norte);
        _centro.Desactivar();

        var respuesta = await LoginAsync("caja");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact(DisplayName = "004/RF-17: el administrador general inicia sesión sin sucursales activas")]
    public async Task General_inicia_sesion_sin_sucursales_activas()
    {
        Sembrar("dueno", Rol.AdministradorGeneral);
        _centro.Desactivar();
        _norte.Desactivar();
        _sur.Desactivar();

        var respuesta = await LoginAsync("dueno");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
