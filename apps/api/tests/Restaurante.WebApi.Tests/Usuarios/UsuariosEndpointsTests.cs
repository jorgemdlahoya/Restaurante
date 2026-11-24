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
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Usuarios;

[Collection(ColeccionApi.Nombre)]
public sealed class UsuariosEndpointsTests : IDisposable
{
    private const string Contrasena = "Clave-de-prueba-123";
    private const string Ruta = "/api/r/la-esquina/usuarios";

    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _restaurante;

    public UsuariosEndpointsTests() => _restaurante = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");

    public void Dispose() => _fabrica.Dispose();

    private Usuario Sembrar(string nombre, Rol rol, bool activo = true)
    {
        var hasher = _fabrica.Services.GetRequiredService<IHasherContrasenas>();
        var usuario = Usuario.Crear(
            _restaurante.Id, nombre, hasher.Hashear(Contrasena), rol, _fabrica.SucursalesPara(rol, _restaurante.Id),
            DateTimeOffset.UtcNow);
        if (!activo)
        {
            usuario.Desactivar();
        }

        _fabrica.Usuarios.Agregar(usuario);
        return usuario;
    }

    private HttpClient ClienteComo(Rol rol, Guid? usuarioId = null)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                _fabrica.EmitirToken(rol, usuarioId: usuarioId, restauranteId: _restaurante.Id));
        return cliente;
    }

    private static StringContent Json(string cuerpo) => new(cuerpo, Encoding.UTF8, "application/json");

    [Fact(DisplayName = "001/RF-11: alta → 201 con Location y sin contraseña")]
    public async Task Alta_responde_201_con_location_y_sin_contrasena()
    {
        // Desde 004/RF-02 el cajero lleva al menos una sucursal.
        _fabrica.SembrarSucursal(_restaurante.Id, "centro", "Centro");
        var cliente = ClienteComo(Rol.AdministradorGeneral);

        var respuesta = await cliente.PostAsJsonAsync(Ruta,
            new { nombreUsuario = "maria", contrasena = Contrasena, rol = "cajero", sucursales = new[] { "centro" } });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(cuerpo);
        var raiz = json.RootElement;
        var id = raiz.GetProperty("id").GetGuid();
        Assert.Equal("maria", raiz.GetProperty("nombreUsuario").GetString());
        Assert.Equal("cajero", raiz.GetProperty("rol").GetString());
        Assert.True(raiz.GetProperty("activo").GetBoolean());
        Assert.Equal($"{Ruta}/{id}", respuesta.Headers.Location?.ToString());
        Assert.DoesNotContain("contrasena", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Contrasena, cuerpo, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "001/RF-12: duplicado → 409")]
    public async Task Duplicado_responde_409()
    {
        Sembrar("maria", Rol.Cajero);
        _fabrica.SembrarSucursal(_restaurante.Id, "centro", "Centro");
        var cliente = ClienteComo(Rol.AdministradorGeneral);

        var respuesta = await cliente.PostAsJsonAsync(Ruta,
            new { nombreUsuario = "MARIA", contrasena = Contrasena, rol = "cajero", sucursales = new[] { "centro" } });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact(DisplayName = "001/RF-13: datos inválidos → 400 por campo")]
    public async Task Datos_invalidos_responden_400_por_campo()
    {
        var cliente = ClienteComo(Rol.AdministradorGeneral);

        await AssertValidacionAsync(await cliente.PostAsync(Ruta, Json("{\"contrasena\":\"x\"}")),
            "nombreUsuario", "El nombre de usuario es obligatorio");
        await AssertValidacionAsync(
            await cliente.PostAsync(Ruta, Json("{\"nombreUsuario\":\"ana\",\"rol\":\"cajero\"}")),
            "contrasena", "La contraseña es obligatoria");
        await AssertValidacionAsync(
            await cliente.PostAsync(Ruta, Json("{\"nombreUsuario\":\"ana\",\"contrasena\":\"corta\",\"rol\":\"cajero\"}")),
            "contrasena", "La contraseña debe tener al menos 8 caracteres");
        await AssertValidacionAsync(
            await cliente.PostAsync(Ruta, Json("{\"nombreUsuario\":\"ana\",\"contrasena\":\"Clave-de-prueba-123\"}")),
            "rol", "El rol debe ser administrador_general, administrador_sucursal, cajero o cocinero");

        var usuario = Sembrar("pepe", Rol.Cajero);
        await AssertValidacionAsync(await cliente.PutAsync($"{Ruta}/{usuario.Id}/estado", Json("{}")),
            "activo", "El estado (activo) es obligatorio");
    }

    [Fact(DisplayName = "001/RF-13: JSON mal formado y rol \"Cajero\" → 400")]
    public async Task Json_mal_formado_y_rol_con_mayusculas_responden_400()
    {
        var cliente = ClienteComo(Rol.AdministradorGeneral);

        await AssertValidacionAsync(await cliente.PostAsync(Ruta, Json("{\"nombreUsuario\":")),
            null, "El cuerpo de la petición no es válido.");
        await AssertValidacionAsync(
            await cliente.PostAsync(Ruta,
                Json("{\"nombreUsuario\":\"ana\",\"contrasena\":\"Clave-de-prueba-123\",\"rol\":\"Cajero\"}")),
            "rol", "El rol debe ser administrador_general, administrador_sucursal, cajero o cocinero");

        var usuario = Sembrar("pepe", Rol.Cajero);
        await AssertValidacionAsync(
            await cliente.PutAsync($"{Ruta}/{usuario.Id}/rol", Json("{\"rol\":\"Cajero\"}")),
            "rol", "El rol debe ser administrador_general, administrador_sucursal, cajero o cocinero");
    }

    [Fact(DisplayName = "001/RF-14: listado → 200")]
    public async Task Listado_responde_200()
    {
        // El token debe ser de un usuario existente (004/RF-14): "jefa" es quien consulta.
        var jefa = Sembrar("jefa", Rol.AdministradorGeneral); // arrancar el host primero siembra al administrador inicial
        var cliente = ClienteComo(Rol.AdministradorGeneral, jefa.Id);
        Sembrar("maria", Rol.Cajero, activo: false);

        var respuesta = await cliente.GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var lista = json.RootElement.EnumerateArray().ToList();
        // Sólo jefa y maria: el administrador de plataforma sembrado al arrancar no pertenece al restaurante
        // (filtro por contexto).
        Assert.Equal(["jefa", "maria"], lista.Select(u => u.GetProperty("nombreUsuario").GetString()));
        var maria = lista[1];
        Assert.False(maria.GetProperty("activo").GetBoolean());
    }

    [Fact(DisplayName = "001/RF-14: un usuario mesero recibe 403 en GET /api/r/{slug}/usuarios")]
    public async Task Mesero_recibe_403_en_listado()
    {
        var respuesta = await ClienteComo(Rol.Cajero).GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact(DisplayName = "001/RF-15: desactivar → 200 con activo=false")]
    public async Task Desactivar_responde_200()
    {
        var objetivo = Sembrar("maria", Rol.Cajero);
        var cliente = ClienteComo(Rol.AdministradorGeneral);

        var respuesta = await cliente.PutAsJsonAsync($"{Ruta}/{objetivo.Id}/estado", new { activo = false });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("activo").GetBoolean());
        Assert.False(objetivo.Activo);
    }

    [Fact(DisplayName = "001/RF-16: cambiar rol → 200")]
    public async Task Cambiar_rol_responde_200()
    {
        var objetivo = Sembrar("maria", Rol.Cajero);
        var cliente = ClienteComo(Rol.AdministradorGeneral);

        var respuesta = await cliente.PutAsJsonAsync($"{Ruta}/{objetivo.Id}/rol", new { rol = "cocinero" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("cocinero", json.RootElement.GetProperty("rol").GetString());
    }

    [Fact(DisplayName = "001/RF-17: último administrador → 409")]
    public async Task Ultimo_administrador_responde_409()
    {
        // El administrador inicial es de plataforma: este es el único administrador general activo. Con el
        // sello de sesión (004/RF-14) quien llama debe ser un administrador general existente y activo, así que
        // el único administrador sólo puede ser él mismo: el cambio de rol choca con "último administrador" y la
        // desactivación con la autoprotección; ambos responden 409.
        var unico = Sembrar("jefe", Rol.AdministradorGeneral);
        var cliente = ClienteComo(Rol.AdministradorGeneral, unico.Id);

        var cambioRol = await cliente.PutAsJsonAsync($"{Ruta}/{unico.Id}/rol", new { rol = "cajero" });
        var desactivar = await cliente.PutAsJsonAsync($"{Ruta}/{unico.Id}/estado", new { activo = false });

        Assert.Equal(HttpStatusCode.Conflict, cambioRol.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, desactivar.StatusCode);
        Assert.True(unico.Activo);
        Assert.Equal(Rol.AdministradorGeneral, unico.Rol);
    }

    [Fact(DisplayName = "001/RF-18: autodesactivación → 409")]
    public async Task Autodesactivacion_responde_409()
    {
        var yo = Sembrar("admin", Rol.AdministradorGeneral);
        Sembrar("otro", Rol.AdministradorGeneral);
        var cliente = ClienteComo(Rol.AdministradorGeneral, yo.Id);

        var respuesta = await cliente.PutAsJsonAsync($"{Ruta}/{yo.Id}/estado", new { activo = false });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.True(yo.Activo);
    }

    [Fact(DisplayName = "001/RF-19: id inexistente → 404")]
    public async Task Id_inexistente_responde_404()
    {
        var cliente = ClienteComo(Rol.AdministradorGeneral);

        var estado = await cliente.PutAsJsonAsync($"{Ruta}/{Guid.NewGuid()}/estado", new { activo = true });
        var rol = await cliente.PutAsJsonAsync($"{Ruta}/{Guid.NewGuid()}/rol", new { rol = "cajero" });

        Assert.Equal(HttpStatusCode.NotFound, estado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, rol.StatusCode);
    }

    [Fact(DisplayName = "001/RF-19: id que no es GUID → 404")]
    public async Task Id_que_no_es_guid_responde_404()
    {
        var cliente = ClienteComo(Rol.AdministradorGeneral);

        var estado = await cliente.PutAsJsonAsync($"{Ruta}/abc/estado", new { activo = true });
        var rol = await cliente.PutAsJsonAsync($"{Ruta}/123/rol", new { rol = "cajero" });

        Assert.Equal(HttpStatusCode.NotFound, estado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, rol.StatusCode);
    }
}
