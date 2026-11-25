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

/// <summary>Alta y lista de usuarios con sucursales y alcance de gestión (004/RF-01..RF-03, RF-07, RF-08, RF-11).</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class UsuariosSucursalesTests : IDisposable
{
    private const string Contrasena = "Clave-de-prueba-123";
    private const string Ruta = "/api/r/la-esquina/usuarios";

    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _restaurante;
    private readonly Sucursal _centro;
    private readonly Sucursal _norte;

    public UsuariosSucursalesTests()
    {
        _restaurante = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        _norte = SembrarSucursal(_restaurante.Id, "norte", "Norte", "N");
        _centro = SembrarSucursal(_restaurante.Id, "centro", "Centro", "C");
    }

    public void Dispose() => _fabrica.Dispose();

    private Sucursal SembrarSucursal(Guid restauranteId, string slug, string nombre, string prefijo) =>
        _fabrica.SembrarSucursal(restauranteId, slug, nombre, prefijo);

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

    private HttpClient ClienteDe(Usuario usuario)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", _fabrica.EmitirToken(usuario.Rol, usuarioId: usuario.Id, restauranteId: usuario.RestauranteId));
        return cliente;
    }

    private static StringContent Json(string cuerpo) => new(cuerpo, Encoding.UTF8, "application/json");

    private static string Alta(string nombre, string rol, string sucursalesJson) =>
        $"{{\"nombreUsuario\":\"{nombre}\",\"contrasena\":\"{Contrasena}\",\"rol\":\"{rol}\",\"sucursales\":{sucursalesJson}}}";

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
        var cuerpo = await CuerpoAsync(respuesta);
        Assert.Equal("Acceso denegado", cuerpo.GetProperty("title").GetString());
    }

    [Fact(DisplayName = "004/RF-01: el administrador general crea un cajero con dos sucursales")]
    public async Task General_crea_cajero_con_dos_sucursales()
    {
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        var respuesta = await cliente.PostAsync(Ruta, Json(Alta("maria", "cajero", "[\"norte\",\"centro\"]")));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuerpo = await CuerpoAsync(respuesta);
        Assert.Equal("maria", cuerpo.GetProperty("nombreUsuario").GetString());
        Assert.Equal("cajero", cuerpo.GetProperty("rol").GetString());
        Assert.True(cuerpo.GetProperty("activo").GetBoolean());
        Assert.True(cuerpo.GetProperty("gestionable").GetBoolean());
        var sucursales = cuerpo.GetProperty("sucursales").EnumerateArray().ToList();
        Assert.Equal(["centro", "norte"], sucursales.Select(s => s.GetProperty("slug").GetString()));
        Assert.Equal(["Centro", "Norte"], sucursales.Select(s => s.GetProperty("nombre").GetString()));
        Assert.All(sucursales, s => Assert.True(s.GetProperty("activa").GetBoolean()));

        var guardado = _fabrica.Usuarios.Usuarios.Single(u => u.NombreUsuario == "maria");
        Assert.Equal(new[] { _centro.Id, _norte.Id }.Order(), guardado.Sucursales.Order());
    }

    [Theory(DisplayName = "004/RF-02: alta con cardinalidad inválida responde 400 en sucursales")]
    [InlineData("administrador_sucursal", "[]", "El administrador de sucursal debe tener exactamente una sucursal")]
    [InlineData("administrador_sucursal", "[\"centro\",\"norte\"]", "El administrador de sucursal debe tener exactamente una sucursal")]
    [InlineData("cajero", "[]", "El cajero y el cocinero deben tener al menos una sucursal")]
    [InlineData("administrador_general", "[\"centro\"]", "El administrador general no lleva sucursales")]
    [InlineData("cocinero", "[\"centro\",\"centro\"]", "Las sucursales no pueden repetirse")]
    public async Task Cardinalidad_invalida_responde_400(string rol, string sucursales, string mensaje)
    {
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        var respuesta = await cliente.PostAsync(Ruta, Json(Alta("maria", rol, sucursales)));

        await AssertValidacionAsync(respuesta, "sucursales", mensaje);
        Assert.DoesNotContain(_fabrica.Usuarios.Usuarios, u => u.NombreUsuario == "maria");
    }

    [Fact(DisplayName = "004/RF-02: alta sin sucursales de un cocinero (campo ausente) responde 400 en sucursales")]
    public async Task Sin_campo_sucursales_responde_400()
    {
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        var respuesta = await cliente.PostAsync(Ruta,
            Json($"{{\"nombreUsuario\":\"maria\",\"contrasena\":\"{Contrasena}\",\"rol\":\"cocinero\"}}"));

        await AssertValidacionAsync(respuesta, "sucursales", "El cajero y el cocinero deben tener al menos una sucursal");
    }

    [Fact(DisplayName = "004/RF-02: alta con una sucursal inexistente responde 400 en sucursales")]
    public async Task Sucursal_inexistente_responde_400()
    {
        // La sucursal de otro restaurante no existe para este (filtro global).
        var otro = _fabrica.SembrarRestaurante("Otro", "otro");
        SembrarSucursal(otro.Id, "lejana", "Lejana", "L");
        var cliente = ClienteDe(Sembrar("jefa", Rol.AdministradorGeneral));

        var fantasma = await cliente.PostAsync(Ruta, Json(Alta("maria", "cajero", "[\"centro\",\"fantasma\"]")));
        var ajena = await cliente.PostAsync(Ruta, Json(Alta("maria", "cajero", "[\"lejana\"]")));

        await AssertValidacionAsync(fantasma, "sucursales", "No existe la sucursal fantasma");
        await AssertValidacionAsync(ajena, "sucursales", "No existe la sucursal lejana");
        Assert.DoesNotContain(_fabrica.Usuarios.Usuarios, u => u.NombreUsuario == "maria");
    }

    [Fact(DisplayName = "004/RF-03: la lista trae todos los usuarios con sus sucursales, ordenada por nombre")]
    public async Task Lista_trae_todos_con_sucursales_ordenada()
    {
        var jefa = Sembrar("jefa", Rol.AdministradorGeneral);
        Sembrar("Zoe", Rol.Cajero, _norte);
        var beto = Sembrar("beto", Rol.Cocinero, _norte, _centro);
        beto.Desactivar();
        Sembrar("ana", Rol.AdministradorSucursal, _centro);
        _norte.Desactivar();

        var respuesta = await ClienteDe(jefa).GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var lista = (await CuerpoAsync(respuesta)).EnumerateArray().ToList();
        Assert.Equal(["ana", "beto", "jefa", "Zoe"], lista.Select(u => u.GetProperty("nombreUsuario").GetString()));
        Assert.Equal(["administrador_sucursal", "cocinero", "administrador_general", "cajero"],
            lista.Select(u => u.GetProperty("rol").GetString()));
        Assert.Equal([true, false, true, true], lista.Select(u => u.GetProperty("activo").GetBoolean()));
        Assert.Equal(["centro"], Slugs(lista[0]));
        Assert.Equal(["centro", "norte"], Slugs(lista[1]));
        Assert.Empty(Slugs(lista[2]));
        Assert.Equal(["norte"], Slugs(lista[3]));
        // Las sucursales asignadas se devuelven aunque estén inactivas.
        Assert.False(lista[3].GetProperty("sucursales")[0].GetProperty("activa").GetBoolean());
        Assert.All(lista, u => Assert.True(u.GetProperty("gestionable").GetBoolean()));
    }

    [Fact(DisplayName = "004/RF-03: un administrador del restaurante A no ve usuarios del B")]
    public async Task Administrador_de_A_no_ve_usuarios_de_B()
    {
        var b = _fabrica.SembrarRestaurante("Resto B", "resto-b");
        var sucursalB = SembrarSucursal(b.Id, "centro-b", "Centro B", "B");
        SembrarEn(b.Id, "intruso", Rol.Cajero, sucursalB);
        SembrarEn(b.Id, "jefe-b", Rol.AdministradorGeneral);
        var jefa = Sembrar("jefa", Rol.AdministradorGeneral);
        Sembrar("maria", Rol.Cajero, _centro);
        var cliente = ClienteDe(jefa);

        var propia = await cliente.GetAsync(Ruta);
        var ajena = await cliente.GetAsync("/api/r/resto-b/usuarios");
        var altaAjena = await cliente.PostAsync("/api/r/resto-b/usuarios", Json(Alta("pepe", "cajero", "[\"centro-b\"]")));

        Assert.Equal(HttpStatusCode.OK, propia.StatusCode);
        var lista = (await CuerpoAsync(propia)).EnumerateArray().ToList();
        Assert.Equal(["jefa", "maria"], lista.Select(u => u.GetProperty("nombreUsuario").GetString()));
        Assert.Equal(HttpStatusCode.NotFound, ajena.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, altaAjena.StatusCode);
        Assert.DoesNotContain(_fabrica.Usuarios.Usuarios, u => u.NombreUsuario == "pepe");
    }

    [Fact(DisplayName = "004/RF-07: el administrador de sucursal sólo ve a su personal, compartido sin gestionable")]
    public async Task Administrador_de_sucursal_solo_ve_su_personal()
    {
        Sembrar("jefa", Rol.AdministradorGeneral);
        var admin = Sembrar("admin-centro", Rol.AdministradorSucursal, _centro);
        Sembrar("ana", Rol.Cajero, _centro);
        Sembrar("compartido", Rol.Cajero, _centro, _norte);
        Sembrar("luis", Rol.Cocinero, _norte);
        Sembrar("admin-norte", Rol.AdministradorSucursal, _norte);
        Sembrar("otro-admin", Rol.AdministradorSucursal, _centro);

        var respuesta = await ClienteDe(admin).GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var lista = (await CuerpoAsync(respuesta)).EnumerateArray().ToList();
        Assert.Equal(["admin-centro", "ana", "compartido", "otro-admin"],
            lista.Select(u => u.GetProperty("nombreUsuario").GetString()));
        Assert.Equal([false, true, false, false], lista.Select(u => u.GetProperty("gestionable").GetBoolean()));
        // El compartido muestra todas sus sucursales, también las ajenas.
        Assert.Equal(["centro", "norte"], Slugs(lista[2]));
    }

    [Fact(DisplayName = "004/RF-26: el administrador de sucursal con su sucursal inactiva recibe 403 en la lista y el alta")]
    public async Task Administrador_con_sucursal_inactiva_recibe_403()
    {
        var admin = Sembrar("admin-centro", Rol.AdministradorSucursal, _centro);
        _centro.Desactivar();
        var cliente = ClienteDe(admin);

        await AssertAccesoDenegadoAsync(await cliente.GetAsync(Ruta));
        await AssertAccesoDenegadoAsync(await cliente.PostAsync(Ruta, Json(Alta("pepe", "cocinero", "[\"centro\"]"))));
    }

    [Fact(DisplayName = "004/RF-08: el administrador de sucursal crea un cocinero en su sucursal")]
    public async Task Administrador_de_sucursal_crea_cocinero()
    {
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));

        var respuesta = await cliente.PostAsync(Ruta, Json(Alta("pepe", "cocinero", "[\"centro\"]")));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuerpo = await CuerpoAsync(respuesta);
        Assert.Equal("cocinero", cuerpo.GetProperty("rol").GetString());
        Assert.Equal(["centro"], Slugs(cuerpo));
        Assert.True(cuerpo.GetProperty("gestionable").GetBoolean());
        var guardado = _fabrica.Usuarios.Usuarios.Single(u => u.NombreUsuario == "pepe");
        Assert.Equal([_centro.Id], guardado.Sucursales);
    }

    [Theory(DisplayName = "004/RF-08: el administrador de sucursal con otra sucursal o con rol administrador responde 403")]
    [InlineData("cocinero", "[\"norte\"]")]
    [InlineData("cocinero", "[\"fantasma\"]")]
    [InlineData("cajero", "[\"centro\",\"norte\"]")]
    [InlineData("cajero", "[\"centro\",\"fantasma\"]")]
    [InlineData("administrador_sucursal", "[\"centro\"]")]
    [InlineData("administrador_general", "[]")]
    public async Task Administrador_de_sucursal_fuera_de_alcance_recibe_403(string rol, string sucursales)
    {
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));

        var respuesta = await cliente.PostAsync(Ruta, Json(Alta("pepe", rol, sucursales)));

        await AssertAccesoDenegadoAsync(respuesta);
        Assert.DoesNotContain(_fabrica.Usuarios.Usuarios, u => u.NombreUsuario == "pepe");
    }

    [Fact(DisplayName = "004/RF-08: el administrador de sucursal recibe primero el 400 de cardinalidad")]
    public async Task Administrador_de_sucursal_recibe_400_antes_que_403()
    {
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));

        var respuesta = await cliente.PostAsync(Ruta, Json(Alta("pepe", "cocinero", "[]")));

        await AssertValidacionAsync(respuesta, "sucursales", "El cajero y el cocinero deben tener al menos una sucursal");
    }

    [Fact(DisplayName = "004/RF-10: el administrador de sucursal no desactiva al personal compartido")]
    public async Task Administrador_de_sucursal_no_desactiva_compartido()
    {
        var compartido = Sembrar("compartido", Rol.Cajero, _centro, _norte);
        var sello = compartido.SelloSesion;
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));

        await AssertAccesoDenegadoAsync(
            await cliente.PutAsJsonAsync($"{Ruta}/{compartido.Id}/estado", new { activo = false }));

        Assert.True(compartido.Activo);
        Assert.Equal(sello, compartido.SelloSesion);
    }

    [Fact(DisplayName = "004/RF-10: el administrador de sucursal no desactiva al administrador general")]
    public async Task Administrador_de_sucursal_no_desactiva_al_general()
    {
        var jefa = Sembrar("jefa", Rol.AdministradorGeneral);
        Sembrar("otra-jefa", Rol.AdministradorGeneral);
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));

        await AssertAccesoDenegadoAsync(await cliente.PutAsJsonAsync($"{Ruta}/{jefa.Id}/estado", new { activo = false }));

        Assert.True(jefa.Activo);
    }

    [Fact(DisplayName = "004/RF-10: el administrador de sucursal no cambia su propio estado")]
    public async Task Administrador_de_sucursal_no_se_gestiona_a_si_mismo()
    {
        var admin = Sembrar("admin-centro", Rol.AdministradorSucursal, _centro);
        var cliente = ClienteDe(admin);

        await AssertAccesoDenegadoAsync(await cliente.PutAsJsonAsync($"{Ruta}/{admin.Id}/estado", new { activo = false }));

        Assert.True(admin.Activo);
    }

    [Fact(DisplayName = "004/RF-10: el administrador de sucursal no asciende a su cajero a administrador general")]
    public async Task Administrador_de_sucursal_no_asigna_rol_administrador()
    {
        var cajero = Sembrar("ana", Rol.Cajero, _centro);
        var sello = cajero.SelloSesion;
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));

        // Desde T-06 el rol se edita con PUT /{id} (sustituye a PUT /{id}/rol).
        await AssertAccesoDenegadoAsync(
            await cliente.PutAsJsonAsync($"{Ruta}/{cajero.Id}", new { rol = "administrador_general", sucursales = Array.Empty<string>() }));

        Assert.Equal(Rol.Cajero, cajero.Rol);
        Assert.Equal([_centro.Id], cajero.Sucursales);
        Assert.Equal(sello, cajero.SelloSesion);
    }

    [Fact(DisplayName = "004/RF-09: el administrador de sucursal desactiva a su cajero exclusivo")]
    public async Task Administrador_de_sucursal_desactiva_su_cajero()
    {
        var cajero = Sembrar("ana", Rol.Cajero, _centro);
        var cliente = ClienteDe(Sembrar("admin-centro", Rol.AdministradorSucursal, _centro));

        var respuesta = await cliente.PutAsJsonAsync($"{Ruta}/{cajero.Id}/estado", new { activo = false });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await CuerpoAsync(respuesta);
        Assert.False(cuerpo.GetProperty("activo").GetBoolean());
        Assert.True(cuerpo.GetProperty("gestionable").GetBoolean());
        Assert.False(cajero.Activo);
    }

    [Theory(DisplayName = "004/RF-11: cajero y cocinero reciben 403 en /usuarios")]
    [InlineData(Rol.Cajero)]
    [InlineData(Rol.Cocinero)]
    public async Task Cajero_y_cocinero_reciben_403(Rol rol)
    {
        var cliente = ClienteDe(Sembrar("personal", rol, _centro));

        await AssertAccesoDenegadoAsync(await cliente.GetAsync(Ruta));
        await AssertAccesoDenegadoAsync(await cliente.PostAsync(Ruta, Json(Alta("pepe", "cocinero", "[\"centro\"]"))));
        await AssertAccesoDenegadoAsync(await cliente.PutAsJsonAsync($"{Ruta}/{Guid.NewGuid()}/estado", new { activo = true }));
        Assert.DoesNotContain(_fabrica.Usuarios.Usuarios, u => u.NombreUsuario == "pepe");
    }
}
