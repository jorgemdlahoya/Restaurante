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

namespace Restaurante.WebApi.Tests.Usuarios;

[Collection(ColeccionApi.Nombre)]
public sealed class UsuariosEndpointsTests : IDisposable
{
    private const string Contrasena = "Clave-de-prueba-123";

    private readonly FabricaApi _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    private Usuario Sembrar(string nombre, Rol rol, bool activo = true)
    {
        var hasher = _fabrica.Services.GetRequiredService<IHasherContrasenas>();
        var usuario = Usuario.Crear(nombre, hasher.Hashear(Contrasena), rol, DateTimeOffset.UtcNow);
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
            new AuthenticationHeaderValue("Bearer", FabricaApi.EmitirToken(rol, usuarioId: usuarioId));
        return cliente;
    }

    private static StringContent Json(string cuerpo) => new(cuerpo, Encoding.UTF8, "application/json");

    [Fact(DisplayName = "001/RF-11: alta → 201 con Location y sin contraseña")]
    public async Task Alta_responde_201_con_location_y_sin_contrasena()
    {
        var cliente = ClienteComo(Rol.Administrador);

        var respuesta = await cliente.PostAsJsonAsync("/api/usuarios",
            new { nombreUsuario = "maria", contrasena = Contrasena, rol = "mesero" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(cuerpo);
        var raiz = json.RootElement;
        var id = raiz.GetProperty("id").GetGuid();
        Assert.Equal("maria", raiz.GetProperty("nombreUsuario").GetString());
        Assert.Equal("mesero", raiz.GetProperty("rol").GetString());
        Assert.True(raiz.GetProperty("activo").GetBoolean());
        Assert.Equal($"/api/usuarios/{id}", respuesta.Headers.Location?.ToString());
        Assert.DoesNotContain("contrasena", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Contrasena, cuerpo, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "001/RF-12: duplicado → 409")]
    public async Task Duplicado_responde_409()
    {
        Sembrar("maria", Rol.Mesero);
        var cliente = ClienteComo(Rol.Administrador);

        var respuesta = await cliente.PostAsJsonAsync("/api/usuarios",
            new { nombreUsuario = "MARIA", contrasena = Contrasena, rol = "caja" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact(DisplayName = "001/RF-13: datos inválidos → 400 por campo")]
    public async Task Datos_invalidos_responden_400_por_campo()
    {
        var cliente = ClienteComo(Rol.Administrador);

        await AssertValidacionAsync(await cliente.PostAsync("/api/usuarios", Json("{\"contrasena\":\"x\"}")),
            "nombreUsuario", "El nombre de usuario es obligatorio");
        await AssertValidacionAsync(
            await cliente.PostAsync("/api/usuarios", Json("{\"nombreUsuario\":\"ana\",\"rol\":\"mesero\"}")),
            "contrasena", "La contraseña es obligatoria");
        await AssertValidacionAsync(
            await cliente.PostAsync("/api/usuarios", Json("{\"nombreUsuario\":\"ana\",\"contrasena\":\"corta\",\"rol\":\"mesero\"}")),
            "contrasena", "La contraseña debe tener al menos 8 caracteres");
        await AssertValidacionAsync(
            await cliente.PostAsync("/api/usuarios", Json("{\"nombreUsuario\":\"ana\",\"contrasena\":\"Clave-de-prueba-123\"}")),
            "rol", "El rol debe ser mesero, cocina, caja o administrador");

        var usuario = Sembrar("pepe", Rol.Mesero);
        await AssertValidacionAsync(await cliente.PutAsync($"/api/usuarios/{usuario.Id}/estado", Json("{}")),
            "activo", "El estado (activo) es obligatorio");
    }

    [Fact(DisplayName = "001/RF-13: JSON mal formado y rol \"Caja\" → 400")]
    public async Task Json_mal_formado_y_rol_con_mayusculas_responden_400()
    {
        var cliente = ClienteComo(Rol.Administrador);

        await AssertValidacionAsync(await cliente.PostAsync("/api/usuarios", Json("{\"nombreUsuario\":")),
            null, "El cuerpo de la petición no es válido.");
        await AssertValidacionAsync(
            await cliente.PostAsync("/api/usuarios",
                Json("{\"nombreUsuario\":\"ana\",\"contrasena\":\"Clave-de-prueba-123\",\"rol\":\"Caja\"}")),
            "rol", "El rol debe ser mesero, cocina, caja o administrador");

        var usuario = Sembrar("pepe", Rol.Mesero);
        await AssertValidacionAsync(
            await cliente.PutAsync($"/api/usuarios/{usuario.Id}/rol", Json("{\"rol\":\"Caja\"}")),
            "rol", "El rol debe ser mesero, cocina, caja o administrador");
    }

    [Fact(DisplayName = "001/RF-14: listado → 200")]
    public async Task Listado_responde_200()
    {
        var cliente = ClienteComo(Rol.Administrador); // arrancar el host primero siembra al administrador inicial
        Sembrar("maria", Rol.Mesero, activo: false);

        var respuesta = await cliente.GetAsync("/api/usuarios");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var lista = json.RootElement.EnumerateArray().ToList();
        Assert.Equal(2, lista.Count); // maria + el administrador inicial sembrado al arrancar
        Assert.Contains(lista, u => u.GetProperty("nombreUsuario").GetString() == "maria"
            && !u.GetProperty("activo").GetBoolean());
    }

    [Fact(DisplayName = "001/RF-14: un usuario mesero recibe 403 en GET /api/usuarios")]
    public async Task Mesero_recibe_403_en_listado()
    {
        var respuesta = await ClienteComo(Rol.Mesero).GetAsync("/api/usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact(DisplayName = "001/RF-15: desactivar → 200 con activo=false")]
    public async Task Desactivar_responde_200()
    {
        var objetivo = Sembrar("maria", Rol.Mesero);
        var cliente = ClienteComo(Rol.Administrador);

        var respuesta = await cliente.PutAsJsonAsync($"/api/usuarios/{objetivo.Id}/estado", new { activo = false });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("activo").GetBoolean());
        Assert.False(objetivo.Activo);
    }

    [Fact(DisplayName = "001/RF-16: cambiar rol → 200")]
    public async Task Cambiar_rol_responde_200()
    {
        var objetivo = Sembrar("maria", Rol.Mesero);
        var cliente = ClienteComo(Rol.Administrador);

        var respuesta = await cliente.PutAsJsonAsync($"/api/usuarios/{objetivo.Id}/rol", new { rol = "cocina" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("cocina", json.RootElement.GetProperty("rol").GetString());
    }

    [Fact(DisplayName = "001/RF-17: último administrador → 409")]
    public async Task Ultimo_administrador_responde_409()
    {
        var cliente = ClienteComo(Rol.Administrador); // sub distinto: no es autodesactivación
        // El arranque siembra al administrador inicial: es el único administrador activo.
        var unico = Assert.Single(_fabrica.Usuarios.Usuarios, u => u.Rol == Rol.Administrador);

        var cambioRol = await cliente.PutAsJsonAsync($"/api/usuarios/{unico.Id}/rol", new { rol = "caja" });
        var desactivar = await cliente.PutAsJsonAsync($"/api/usuarios/{unico.Id}/estado", new { activo = false });

        Assert.Equal(HttpStatusCode.Conflict, cambioRol.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, desactivar.StatusCode);
        Assert.True(unico.Activo);
        Assert.Equal(Rol.Administrador, unico.Rol);
    }

    [Fact(DisplayName = "001/RF-18: autodesactivación → 409")]
    public async Task Autodesactivacion_responde_409()
    {
        var yo = Sembrar("admin", Rol.Administrador);
        Sembrar("otro", Rol.Administrador);
        var cliente = ClienteComo(Rol.Administrador, yo.Id);

        var respuesta = await cliente.PutAsJsonAsync($"/api/usuarios/{yo.Id}/estado", new { activo = false });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.True(yo.Activo);
    }

    [Fact(DisplayName = "001/RF-19: id inexistente → 404")]
    public async Task Id_inexistente_responde_404()
    {
        var cliente = ClienteComo(Rol.Administrador);

        var estado = await cliente.PutAsJsonAsync($"/api/usuarios/{Guid.NewGuid()}/estado", new { activo = true });
        var rol = await cliente.PutAsJsonAsync($"/api/usuarios/{Guid.NewGuid()}/rol", new { rol = "caja" });

        Assert.Equal(HttpStatusCode.NotFound, estado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, rol.StatusCode);
    }

    [Fact(DisplayName = "001/RF-19: id que no es GUID → 404")]
    public async Task Id_que_no_es_guid_responde_404()
    {
        var cliente = ClienteComo(Rol.Administrador);

        var estado = await cliente.PutAsJsonAsync("/api/usuarios/abc/estado", new { activo = true });
        var rol = await cliente.PutAsJsonAsync("/api/usuarios/123/rol", new { rol = "caja" });

        Assert.Equal(HttpStatusCode.NotFound, estado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, rol.StatusCode);
    }
}
