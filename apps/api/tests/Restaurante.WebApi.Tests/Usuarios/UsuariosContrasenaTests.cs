using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using static Restaurante.WebApi.Tests.Infraestructura.AfirmacionesHttp;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Usuarios;

/// <summary>Asignación de contraseña nueva con <c>PUT /api/r/{slug}/usuarios/{id}/contrasena</c> (004/RF-06, RF-10, RF-13).</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class UsuariosContrasenaTests : IDisposable
{
    private const string Contrasena = "Clave-de-prueba-123";
    private const string ContrasenaNueva = "Clave-nueva-456";
    private const string Ruta = "/api/r/la-esquina/usuarios";

    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _restaurante;
    private readonly Sucursal _norte;
    private readonly Sucursal _centro;

    public UsuariosContrasenaTests()
    {
        _restaurante = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        _norte = _fabrica.SembrarSucursal(_restaurante.Id, "norte", "Norte", "N");
        _centro = _fabrica.SembrarSucursal(_restaurante.Id, "centro", "Centro", "C");
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

    private string TokenDe(Usuario usuario) =>
        _fabrica.EmitirToken(usuario.Rol, usuarioId: usuario.Id, restauranteId: usuario.RestauranteId);

    private HttpClient ClienteCon(string token)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    private HttpClient ClienteDe(Usuario usuario) => ClienteCon(TokenDe(usuario));

    private static Task<HttpResponseMessage> AsignarAsync(HttpClient cliente, Guid id, string contrasena) =>
        cliente.PutAsJsonAsync($"{Ruta}/{id}/contrasena", new { contrasena });

    private Task<HttpResponseMessage> LoginAsync(string nombre, string contrasena) =>
        _fabrica.CreateClient().PostAsJsonAsync(
            "/api/r/la-esquina/auth/login", new { nombreUsuario = nombre, contrasena });

    private static async Task AssertSesionInvalidaAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("Bearer", Assert.Single(respuesta.Headers.WwwAuthenticate).Scheme);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("Sesión no válida", json.RootElement.GetProperty("title").GetString());
    }

    [Fact(DisplayName = "004/RF-06: el administrador general asigna una contraseña nueva y el usuario entra con ella")]
    public async Task Asigna_contrasena_y_el_usuario_entra_con_ella()
    {
        var cajero = Sembrar("pepe", Rol.Cajero, _centro);
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        var respuesta = await AsignarAsync(cliente, cajero.Id, ContrasenaNueva);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("pepe", ContrasenaNueva)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("pepe", Contrasena)).StatusCode);
    }

    [Fact(DisplayName = "004/RF-06: una contraseña de 7 caracteres responde 400")]
    public async Task Contrasena_de_siete_caracteres_responde_400()
    {
        var cajero = Sembrar("pepe", Rol.Cajero, _centro);
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));
        var sello = cajero.SelloSesion;

        await AssertValidacionAsync(await AsignarAsync(cliente, cajero.Id, "1234567"),
            "contrasena", "La contraseña debe tener al menos 8 caracteres");

        Assert.Equal(sello, cajero.SelloSesion);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("pepe", Contrasena)).StatusCode);
    }

    [Fact(DisplayName = "004/RF-06: sin contraseña responde 400")]
    public async Task Sin_contrasena_responde_400()
    {
        var cajero = Sembrar("pepe", Rol.Cajero, _centro);
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        await AssertValidacionAsync(
            await cliente.PutAsJsonAsync($"{Ruta}/{cajero.Id}/contrasena", new { }),
            "contrasena", "La contraseña es obligatoria");
    }

    [Fact(DisplayName = "004/RF-13: asignar contraseña invalida el token anterior del usuario")]
    public async Task Asignar_contrasena_invalida_el_token_anterior()
    {
        var admin = Sembrar("admin-centro", Rol.AdministradorSucursal, _centro);
        var tokenAnterior = TokenDe(admin);
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));
        Assert.Equal(HttpStatusCode.OK, (await ClienteCon(tokenAnterior).GetAsync(Ruta)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await AsignarAsync(cliente, admin.Id, ContrasenaNueva)).StatusCode);

        await AssertSesionInvalidaAsync(await ClienteCon(tokenAnterior).GetAsync(Ruta));
    }

    [Fact(DisplayName = "004/RF-06: el administrador general puede asignarse contraseña y su token anterior deja de valer")]
    public async Task El_general_se_asigna_contrasena_y_su_token_deja_de_valer()
    {
        var yo = Sembrar("jefa", Rol.AdministradorGeneral);
        var tokenAnterior = TokenDe(yo);

        var respuesta = await AsignarAsync(ClienteCon(tokenAnterior), yo.Id, ContrasenaNueva);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        await AssertSesionInvalidaAsync(await ClienteCon(tokenAnterior).GetAsync(Ruta));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("jefa", ContrasenaNueva)).StatusCode);
    }

    [Fact(DisplayName = "004/RF-10: el administrador de sucursal asigna contraseña a un cajero exclusivo de su sucursal")]
    public async Task El_administrador_de_sucursal_asigna_a_su_personal()
    {
        var cajero = Sembrar("pepe", Rol.Cajero, _centro);
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));

        Assert.Equal(HttpStatusCode.NoContent, (await AsignarAsync(cliente, cajero.Id, ContrasenaNueva)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("pepe", ContrasenaNueva)).StatusCode);
    }

    [Fact(DisplayName = "004/RF-10: el administrador de sucursal recibe 403 al cambiar la contraseña de personal compartido")]
    public async Task El_administrador_de_sucursal_recibe_403_con_personal_compartido()
    {
        var compartido = Sembrar("compartido", Rol.Cajero, _centro, _norte);
        var otroAdmin = Sembrar("admin-norte", Rol.AdministradorSucursal, _norte);
        var admin = Sembrar("admin-centro", Rol.AdministradorSucursal, _centro);
        var jefa = Sembrar("jefa", Rol.AdministradorGeneral);
        var cliente = ClienteDe(admin);

        foreach (var objetivo in new[] { compartido, otroAdmin, admin, jefa })
        {
            var sello = objetivo.SelloSesion;

            var respuesta = await AsignarAsync(cliente, objetivo.Id, ContrasenaNueva);

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal(sello, objetivo.SelloSesion);
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(objetivo.NombreUsuario, Contrasena)).StatusCode);
        }
    }

    [Fact(DisplayName = "004/RF-06: un usuario inexistente responde 404")]
    public async Task Usuario_inexistente_responde_404()
    {
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        Assert.Equal(HttpStatusCode.NotFound, (await AsignarAsync(cliente, Guid.NewGuid(), ContrasenaNueva)).StatusCode);
    }
}
