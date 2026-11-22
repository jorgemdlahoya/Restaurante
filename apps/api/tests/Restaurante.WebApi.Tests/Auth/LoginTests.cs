using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using static Restaurante.WebApi.Tests.Infraestructura.AfirmacionesHttp;

namespace Restaurante.WebApi.Tests.Auth;

[Collection(ColeccionApi.Nombre)]
public sealed class LoginTests : IDisposable
{
    private const string Contrasena = "Clave-de-prueba-123";
    private const string MensajeCredenciales = "Usuario o contraseña incorrectos";

    private readonly FabricaApi _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    private Usuario Sembrar(string nombre, Rol rol, bool activo = true)
    {
        var hasher = _fabrica.Services.GetRequiredService<IHasherContrasenas>();
        var usuario = Usuario.Crear(FabricaApi.RestaurantePruebaId, nombre, hasher.Hashear(Contrasena), rol, DateTimeOffset.UtcNow);
        if (!activo)
        {
            usuario.Desactivar();
        }

        _fabrica.Usuarios.Agregar(usuario);
        return usuario;
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient cliente, string cuerpoJson) =>
        cliente.PostAsync("/api/auth/login",
            new StringContent(cuerpoJson, Encoding.UTF8, "application/json"));

    [Fact(DisplayName = "001/RF-01: login correcto devuelve token utilizable en un endpoint protegido")]
    public async Task Login_correcto_devuelve_token_utilizable()
    {
        Sembrar("cajero", Rol.Cajero);
        var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login",
            new { nombreUsuario = "cajero", contrasena = Contrasena });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        var token = raiz.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal("cajero", raiz.GetProperty("nombreUsuario").GetString());
        Assert.Equal("cajero", raiz.GetProperty("rol").GetString());
        Assert.True(raiz.TryGetProperty("expiraEn", out _));

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var protegido = await cliente.GetAsync("/prueba/caja");
        Assert.Equal(HttpStatusCode.OK, protegido.StatusCode);
    }

    [Fact(DisplayName = "001/RF-02: credenciales incorrectas → 401 con \"Usuario o contraseña incorrectos\"")]
    public async Task Credenciales_incorrectas_responden_401()
    {
        Sembrar("cajero", Rol.Cajero);
        var cliente = _fabrica.CreateClient();

        var malaContrasena = await cliente.PostAsJsonAsync("/api/auth/login",
            new { nombreUsuario = "cajero", contrasena = "otra-cosa" });
        var usuarioInexistente = await cliente.PostAsJsonAsync("/api/auth/login",
            new { nombreUsuario = "nadie", contrasena = Contrasena });

        foreach (var respuesta in new[] { malaContrasena, usuarioInexistente })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            Assert.Equal(MensajeCredenciales, json.RootElement.GetProperty("detail").GetString());
        }
    }

    [Fact(DisplayName = "001/RF-03: usuario desactivado → 401 con el mismo mensaje")]
    public async Task Usuario_desactivado_responde_401()
    {
        Sembrar("baja", Rol.Cajero, activo: false);
        var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login",
            new { nombreUsuario = "baja", contrasena = Contrasena });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(MensajeCredenciales, json.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "001/RF-04: cuerpo sin contraseña → 400 ValidationProblemDetails con el campo")]
    public async Task Cuerpo_sin_contrasena_responde_400()
    {
        var cliente = _fabrica.CreateClient();

        var respuesta = await LoginAsync(cliente, "{\"nombreUsuario\":\"cajero\"}");

        await AssertValidacionAsync(respuesta, "contrasena", "La contraseña es obligatoria");
    }

    [Fact(DisplayName = "001/RF-04: contraseña vacía → 400 con la misma forma")]
    public async Task Contrasena_vacia_responde_400()
    {
        var respuesta = await LoginAsync(_fabrica.CreateClient(),
            "{\"nombreUsuario\":\"cajero\",\"contrasena\":\"\"}");

        await AssertValidacionAsync(respuesta, "contrasena", "La contraseña es obligatoria");
    }

    [Fact(DisplayName = "001/RF-04: cuerpo sin nombre de usuario → 400 con la misma forma")]
    public async Task Cuerpo_sin_nombre_responde_400()
    {
        var respuesta = await LoginAsync(_fabrica.CreateClient(), "{\"contrasena\":\"x\"}");

        await AssertValidacionAsync(respuesta, "nombreUsuario", "El nombre de usuario es obligatorio");
    }

    [Fact(DisplayName = "001/RF-04: JSON mal formado → 400 con la misma forma y mensaje en español")]
    public async Task Json_mal_formado_responde_400()
    {
        var respuesta = await LoginAsync(_fabrica.CreateClient(), "{\"nombreUsuario\":");

        await AssertValidacionAsync(respuesta, null, "El cuerpo de la petición no es válido.");
    }

    [Fact(DisplayName = "001/RF-07: login no exige token")]
    public async Task Login_no_exige_token()
    {
        var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login",
            new { nombreUsuario = "nadie", contrasena = "x" });

        // El 401 proviene de las credenciales (detail del handler), no de la falta de token.
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(MensajeCredenciales, json.RootElement.GetProperty("detail").GetString());
    }
}
