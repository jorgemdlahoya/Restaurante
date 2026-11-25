using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using static Restaurante.WebApi.Tests.Infraestructura.AfirmacionesHttp;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Usuarios;

/// <summary>
/// Edición de rol y sucursales con <c>PUT /api/r/{slug}/usuarios/{id}</c> y desactivación con alcance
/// (004/RF-04, RF-05, RF-09, RF-10, RF-13, RF-26, RNF-02).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public sealed class UsuariosEdicionTests : IDisposable
{
    private const string Contrasena = "Clave-de-prueba-123";
    private const string Ruta = "/api/r/la-esquina/usuarios";

    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _restaurante;
    private readonly Sucursal _centro;
    private readonly Sucursal _norte;

    public UsuariosEdicionTests()
    {
        _restaurante = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        _norte = _fabrica.SembrarSucursal(_restaurante.Id, "norte", "Norte", "N");
        _centro = _fabrica.SembrarSucursal(_restaurante.Id, "centro", "Centro", "C");
    }

    public void Dispose() => _fabrica.Dispose();

    private Usuario Sembrar(string nombre, Rol rol, params Sucursal[] sucursales) =>
        SembrarEn(_restaurante.Id, nombre, rol, sucursales);

    private Usuario SembrarEn(Guid restauranteId, string nombre, Rol rol, params Sucursal[] sucursales)
    {
        var hasher = _fabrica.Services.GetRequiredService<IHasherContrasenas>();
        var usuario = Usuario.Crear(
            restauranteId, nombre, hasher.Hashear(Contrasena), rol, sucursales.Select(s => s.Id).ToList(),
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

    private static StringContent Json(string cuerpo) => new(cuerpo, Encoding.UTF8, "application/json");

    private static Task<HttpResponseMessage> EditarAsync(
        HttpClient cliente, Guid id, string rol, params string[] sucursales) =>
        cliente.PutAsJsonAsync($"{Ruta}/{id}", new { rol, sucursales });

    private static async Task<JsonElement> CuerpoAsync(HttpResponseMessage respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    private static string[] Slugs(JsonElement usuario) =>
        usuario.GetProperty("sucursales").EnumerateArray().Select(s => s.GetProperty("slug").GetString()!).ToArray();

    private static async Task AssertAccesoDenegadoAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Acceso denegado", (await CuerpoAsync(respuesta)).GetProperty("title").GetString());
    }

    private static async Task AssertConflictoAsync(HttpResponseMessage respuesta, string detalle)
    {
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(detalle, (await CuerpoAsync(respuesta)).GetProperty("detail").GetString());
    }

    private static async Task AssertSesionInvalidaAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("Bearer", Assert.Single(respuesta.Headers.WwwAuthenticate).Scheme);
        Assert.Equal("Sesión no válida", (await CuerpoAsync(respuesta)).GetProperty("title").GetString());
    }

    [Fact(DisplayName = "004/RF-04: el administrador general cambia rol y sucursales")]
    public async Task General_cambia_rol_y_sucursales()
    {
        var cajero = Sembrar("maria", Rol.Cajero, _centro);
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        var respuesta = await EditarAsync(cliente, cajero.Id, "cocinero", "norte", "centro");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await CuerpoAsync(respuesta);
        Assert.Equal("cocinero", cuerpo.GetProperty("rol").GetString());
        Assert.Equal(["centro", "norte"], Slugs(cuerpo));
        Assert.True(cuerpo.GetProperty("gestionable").GetBoolean());
        Assert.Equal(Rol.Cocinero, cajero.Rol);
        Assert.Equal(new[] { _centro.Id, _norte.Id }.Order(), cajero.Sucursales.Order());

        // Ascender a administrador de sucursal con una única sucursal.
        var ascenso = await EditarAsync(cliente, cajero.Id, "administrador_sucursal", "norte");
        Assert.Equal(HttpStatusCode.OK, ascenso.StatusCode);
        Assert.Equal(["norte"], Slugs(await CuerpoAsync(ascenso)));
        Assert.Equal(Rol.AdministradorSucursal, cajero.Rol);
    }

    [Fact(DisplayName = "004/RF-04: el nombre de usuario no cambia")]
    public async Task El_nombre_de_usuario_no_cambia()
    {
        var cajero = Sembrar("maria", Rol.Cajero, _centro);
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        var respuesta = await cliente.PutAsync($"{Ruta}/{cajero.Id}",
            Json("{\"nombreUsuario\":\"otro-nombre\",\"rol\":\"cocinero\",\"sucursales\":[\"centro\"]}"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("maria", (await CuerpoAsync(respuesta)).GetProperty("nombreUsuario").GetString());
        Assert.Equal("maria", cajero.NombreUsuario);
        Assert.Equal(Rol.Cocinero, cajero.Rol);
        Assert.DoesNotContain(_fabrica.Usuarios.Usuarios, u => u.NombreUsuario == "otro-nombre");
    }

    [Theory(DisplayName = "004/RF-04: editar con cardinalidad inválida o sucursal inexistente responde 400 en sucursales")]
    [InlineData("administrador_general", "[\"centro\"]", "El administrador general no lleva sucursales")]
    [InlineData("administrador_sucursal", "[\"centro\",\"norte\"]", "El administrador de sucursal debe tener exactamente una sucursal")]
    [InlineData("cajero", "[]", "El cajero y el cocinero deben tener al menos una sucursal")]
    [InlineData("cocinero", "[\"centro\",\"centro\"]", "Las sucursales no pueden repetirse")]
    [InlineData("cocinero", "[\"fantasma\"]", "No existe la sucursal fantasma")]
    public async Task Cardinalidad_invalida_responde_400(string rol, string sucursales, string mensaje)
    {
        var cajero = Sembrar("maria", Rol.Cajero, _centro);
        var sello = cajero.SelloSesion;
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        var respuesta = await cliente.PutAsync($"{Ruta}/{cajero.Id}",
            Json($"{{\"rol\":\"{rol}\",\"sucursales\":{sucursales}}}"));

        await AssertValidacionAsync(respuesta, "sucursales", mensaje);
        Assert.Equal(Rol.Cajero, cajero.Rol);
        Assert.Equal([_centro.Id], cajero.Sucursales);
        Assert.Equal(sello, cajero.SelloSesion);
    }

    [Fact(DisplayName = "004/RF-04: editar sin rol válido responde 400 en rol")]
    public async Task Rol_invalido_responde_400()
    {
        var cajero = Sembrar("maria", Rol.Cajero, _centro);
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        await AssertValidacionAsync(
            await cliente.PutAsync($"{Ruta}/{cajero.Id}", Json("{\"sucursales\":[\"centro\"]}")),
            "rol", "El rol debe ser administrador_general, administrador_sucursal, cajero o cocinero");
        await AssertValidacionAsync(
            await EditarAsync(cliente, cajero.Id, "administrador_plataforma"),
            "rol", "El rol debe ser administrador_general, administrador_sucursal, cajero o cocinero");
    }

    [Fact(DisplayName = "004/RF-04: editar un usuario de otro restaurante responde 404")]
    public async Task Usuario_de_otro_restaurante_responde_404()
    {
        var b = _fabrica.SembrarRestaurante("Resto B", "resto-b");
        var sucursalB = _fabrica.SembrarSucursal(b.Id, "centro-b", "Centro B", "B");
        var deB = SembrarEn(b.Id, "beto", Rol.Cajero, sucursalB);
        var sello = deB.SelloSesion;
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        var propia = await EditarAsync(cliente, deB.Id, "cocinero", "centro");
        var ajena = await cliente.PutAsJsonAsync(
            $"/api/r/resto-b/usuarios/{deB.Id}", new { rol = "cocinero", sucursales = new[] { "centro-b" } });

        Assert.Equal(HttpStatusCode.NotFound, propia.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, ajena.StatusCode);
        Assert.Equal(Rol.Cajero, deB.Rol);
        Assert.Equal([sucursalB.Id], deB.Sucursales);
        Assert.Equal(sello, deB.SelloSesion);
    }

    [Fact(DisplayName = "004/RF-05: quitar el rol al último administrador general responde 409")]
    public async Task Quitar_el_rol_al_ultimo_administrador_responde_409()
    {
        // Quien llama es un administrador general activo (RF-14), así que el último sólo puede ser él mismo: la
        // regla de autoprotección responde antes. La del último administrador sin autoría propia la cubre
        // EditarUsuarioTests (Application).
        var unico = Sembrar("jefe", Rol.AdministradorGeneral);
        var cliente = ClienteDe(unico);

        await AssertConflictoAsync(
            await EditarAsync(cliente, unico.Id, "administrador_sucursal", "centro"),
            "No puedes quitarte el rol de administrador general");

        Assert.Equal(Rol.AdministradorGeneral, unico.Rol);
        Assert.Empty(unico.Sucursales);
    }

    [Fact(DisplayName = "004/RF-05: quitarse a sí mismo el rol responde 409")]
    public async Task Quitarse_el_rol_a_si_mismo_responde_409()
    {
        Sembrar("otra", Rol.AdministradorGeneral);
        var yo = Sembrar("jefe", Rol.AdministradorGeneral);
        var token = TokenDe(yo);
        var cliente = ClienteCon(token);

        await AssertConflictoAsync(
            await EditarAsync(cliente, yo.Id, "cajero", "centro"), "No puedes quitarte el rol de administrador general");

        Assert.Equal(Rol.AdministradorGeneral, yo.Rol);
        // Nada cambió: su sesión sigue valiendo.
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(Ruta)).StatusCode);
    }

    [Fact(DisplayName = "004/RF-05: desactivarse a sí mismo responde 409")]
    public async Task Desactivarse_a_si_mismo_responde_409()
    {
        Sembrar("otra", Rol.AdministradorGeneral);
        var yo = Sembrar("jefe", Rol.AdministradorGeneral);
        var cliente = ClienteDe(yo);

        await AssertConflictoAsync(
            await cliente.PutAsJsonAsync($"{Ruta}/{yo.Id}/estado", new { activo = false }),
            "No puedes desactivar tu propio usuario");

        Assert.True(yo.Activo);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(Ruta)).StatusCode);
    }

    [Fact(DisplayName = "004/RF-09: el administrador de sucursal edita y desactiva a su cajero exclusivo")]
    public async Task Administrador_de_sucursal_edita_y_desactiva_su_cajero()
    {
        var cajero = Sembrar("ana", Rol.Cajero, _centro);
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));

        var edicion = await EditarAsync(cliente, cajero.Id, "cocinero", "CENTRO");

        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);
        var cuerpo = await CuerpoAsync(edicion);
        Assert.Equal("cocinero", cuerpo.GetProperty("rol").GetString());
        Assert.Equal(["centro"], Slugs(cuerpo));
        Assert.True(cuerpo.GetProperty("gestionable").GetBoolean());
        Assert.Equal(Rol.Cocinero, cajero.Rol);

        var desactivacion = await cliente.PutAsJsonAsync($"{Ruta}/{cajero.Id}/estado", new { activo = false });

        Assert.Equal(HttpStatusCode.OK, desactivacion.StatusCode);
        Assert.False((await CuerpoAsync(desactivacion)).GetProperty("activo").GetBoolean());
        Assert.False(cajero.Activo);
    }

    [Fact(DisplayName = "004/RF-10: el administrador de sucursal recibe 403 con personal compartido, otro administrador o él mismo, y nada cambia")]
    public async Task Administrador_de_sucursal_recibe_403_fuera_de_alcance()
    {
        var admin = Sembrar("admin-centro", Rol.AdministradorSucursal, _centro);
        var compartido = Sembrar("compartido", Rol.Cajero, _centro, _norte);
        var otroAdmin = Sembrar("otro-admin", Rol.AdministradorSucursal, _centro);
        var jefa = Sembrar("jefa", Rol.AdministradorGeneral);
        Sembrar("otra-jefa", Rol.AdministradorGeneral);
        var cliente = ClienteDe(admin);

        foreach (var objetivo in new[] { compartido, otroAdmin, admin, jefa })
        {
            var rol = objetivo.Rol;
            var sucursales = objetivo.Sucursales;
            var sello = objetivo.SelloSesion;

            await AssertAccesoDenegadoAsync(await EditarAsync(cliente, objetivo.Id, "cocinero", "centro"));
            await AssertAccesoDenegadoAsync(
                await cliente.PutAsJsonAsync($"{Ruta}/{objetivo.Id}/estado", new { activo = false }));

            Assert.Equal(rol, objetivo.Rol);
            Assert.Equal(sucursales, objetivo.Sucursales);
            Assert.Equal(sello, objetivo.SelloSesion);
            Assert.True(objetivo.Activo);
        }
    }

    [Theory(DisplayName = "004/RF-10: el administrador de sucursal no asigna otro rol ni otra sucursal a su cajero (403)")]
    [InlineData("administrador_general", new string[0])]
    [InlineData("administrador_sucursal", new[] { "centro" })]
    [InlineData("cajero", new[] { "norte" })]
    [InlineData("cajero", new[] { "fantasma" })]
    [InlineData("cajero", new[] { "centro", "norte" })]
    public async Task Administrador_de_sucursal_no_asigna_fuera_de_alcance(string rol, string[] sucursales)
    {
        var cajero = Sembrar("ana", Rol.Cajero, _centro);
        var sello = cajero.SelloSesion;
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));

        await AssertAccesoDenegadoAsync(await EditarAsync(cliente, cajero.Id, rol, sucursales));

        Assert.Equal(Rol.Cajero, cajero.Rol);
        Assert.Equal([_centro.Id], cajero.Sucursales);
        Assert.Equal(sello, cajero.SelloSesion);
    }

    [Fact(DisplayName = "004/RF-10: el administrador de sucursal recibe primero el 400 de cardinalidad")]
    public async Task Administrador_de_sucursal_recibe_400_antes_que_403()
    {
        var compartido = Sembrar("compartido", Rol.Cajero, _centro, _norte);
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));

        await AssertValidacionAsync(await EditarAsync(cliente, compartido.Id, "cocinero"),
            "sucursales", "El cajero y el cocinero deben tener al menos una sucursal");
    }

    [Fact(DisplayName = "004/RF-26: el administrador de sucursal con su sucursal inactiva recibe 403 al editar, incluso un id inexistente")]
    public async Task Administrador_con_sucursal_inactiva_recibe_403()
    {
        var cajero = Sembrar("ana", Rol.Cajero, _centro);
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));
        _centro.Desactivar();

        await AssertAccesoDenegadoAsync(await EditarAsync(cliente, cajero.Id, "cocinero", "centro"));
        await AssertAccesoDenegadoAsync(await EditarAsync(cliente, Guid.NewGuid(), "cocinero", "centro"));
        Assert.Equal(Rol.Cajero, cajero.Rol);
    }

    [Fact(DisplayName = "004/RF-13: editar sin cambios no invalida el token del usuario editado")]
    public async Task Editar_sin_cambios_no_invalida_el_token()
    {
        var editado = Sembrar("admin-centro", Rol.AdministradorSucursal, _centro);
        var tokenEditado = TokenDe(editado);
        var cajeroCompartido = Sembrar("compartido", Rol.Cajero, _centro, _norte);
        var tokenCompartido = TokenDe(cajeroCompartido);
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        Assert.Equal(HttpStatusCode.OK, (await EditarAsync(cliente, editado.Id, "administrador_sucursal", "centro")).StatusCode);
        // Mismo rol y mismas sucursales en otro orden.
        Assert.Equal(HttpStatusCode.OK, (await EditarAsync(cliente, cajeroCompartido.Id, "cajero", "norte", "centro")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await ClienteCon(tokenEditado).GetAsync(Ruta)).StatusCode);
        // El cajero no gestiona usuarios: 403 (y no 401) prueba que su sesión sigue siendo válida.
        await AssertAccesoDenegadoAsync(await ClienteCon(tokenCompartido).GetAsync(Ruta));
    }

    [Fact(DisplayName = "004/RF-13: cambiar sus sucursales sí invalida el token del usuario editado")]
    public async Task Cambiar_sus_sucursales_invalida_el_token()
    {
        var editado = Sembrar("admin-centro", Rol.AdministradorSucursal, _centro);
        var tokenAnterior = TokenDe(editado);
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));
        Assert.Equal(HttpStatusCode.OK, (await ClienteCon(tokenAnterior).GetAsync(Ruta)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await EditarAsync(cliente, editado.Id, "administrador_sucursal", "norte")).StatusCode);

        await AssertSesionInvalidaAsync(await ClienteCon(tokenAnterior).GetAsync(Ruta));
        // Un token nuevo, emitido tras el cambio, funciona.
        Assert.Equal(HttpStatusCode.OK, (await ClienteDe(editado).GetAsync(Ruta)).StatusCode);
    }

    [Fact(DisplayName = "004/RF-13: cambiar su rol invalida el token del usuario editado")]
    public async Task Cambiar_su_rol_invalida_el_token()
    {
        var editado = Sembrar("ana", Rol.Cajero, _centro);
        var tokenAnterior = TokenDe(editado);
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        Assert.Equal(HttpStatusCode.OK, (await EditarAsync(cliente, editado.Id, "cocinero", "centro")).StatusCode);

        await AssertSesionInvalidaAsync(await ClienteCon(tokenAnterior).GetAsync(Ruta));
    }
}
