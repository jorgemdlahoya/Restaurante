using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.WebApi.Tests.Restaurantes;

/// <summary>Aislamiento por restaurante: el contexto sale del token y la ruta <c>/api/r/{slug}</c> debe coincidir.</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class AislamientoTests : IDisposable
{
    private const string Contrasena = "Clave-de-prueba-123";

    private readonly FabricaApi _fabrica = new();
    private readonly RestauranteEntidad _a;
    private readonly RestauranteEntidad _b;

    public AislamientoTests()
    {
        _a = _fabrica.SembrarRestaurante("Resto A", "resto-a");
        _b = _fabrica.SembrarRestaurante("Resto B", "resto-b");
    }

    public void Dispose() => _fabrica.Dispose();

    private Usuario Sembrar(Guid restauranteId, string nombre, Rol rol)
    {
        var hasher = _fabrica.Services.GetRequiredService<IHasherContrasenas>();
        var usuario = Usuario.Crear(
            restauranteId, nombre, hasher.Hashear(Contrasena), rol, _fabrica.SucursalesPara(rol, restauranteId),
            DateTimeOffset.UtcNow);
        _fabrica.Usuarios.Agregar(usuario);
        return usuario;
    }

    private HttpClient Cliente(string token)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    private readonly Dictionary<Guid, Usuario> _admins = [];

    /// <summary>
    /// Cliente del administrador "admin" del restaurante (se siembra la primera vez): el token debe ser de un
    /// usuario existente con su sello (004/RF-14), así que "admin" aparece en los listados de ese restaurante.
    /// </summary>
    private HttpClient AdminDe(RestauranteEntidad restaurante)
    {
        if (!_admins.TryGetValue(restaurante.Id, out var admin))
        {
            admin = Sembrar(restaurante.Id, "admin", Rol.AdministradorGeneral);
            _admins[restaurante.Id] = admin;
        }

        return Cliente(_fabrica.EmitirToken(Rol.AdministradorGeneral, usuarioId: admin.Id, restauranteId: restaurante.Id));
    }

    private static async Task<List<string?>> NombresAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.EnumerateArray().Select(u => u.GetProperty("nombreUsuario").GetString()).ToList();
    }

    private static async Task AssertProblemaAsync(
        HttpResponseMessage respuesta, HttpStatusCode estado, string titulo, string? detalle = null)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal((int)estado, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(titulo, json.RootElement.GetProperty("title").GetString());
        if (detalle is not null)
        {
            Assert.Equal(detalle, json.RootElement.GetProperty("detail").GetString());
        }
    }

    [Fact(DisplayName = "002/RF-06: tokens de un restaurante desactivado reciben 401 y vuelven a valer al reactivarlo")]
    public async Task Restaurante_desactivado_da_401_y_reactivado_vuelve_a_valer()
    {
        Sembrar(_a.Id, "ana", Rol.Cajero);
        var cliente = AdminDe(_a);

        var antes = await NombresAsync(await cliente.GetAsync("/api/r/resto-a/usuarios"));

        _a.Desactivar();
        var desactivado = await cliente.GetAsync("/api/r/resto-a/usuarios");
        await AssertProblemaAsync(desactivado, HttpStatusCode.Unauthorized, "No autenticado", "El restaurante no está activo.");
        Assert.Contains("Bearer", desactivado.Headers.WwwAuthenticate.Select(h => h.Scheme));

        _a.Activar();
        var despues = await NombresAsync(await cliente.GetAsync("/api/r/resto-a/usuarios"));

        Assert.Equal(["admin", "ana"], antes);
        Assert.Equal(antes, despues);
    }

    [Fact(DisplayName = "002/RF-06: token con restaurant_id desconocido recibe 401")]
    public async Task Token_con_restaurante_desconocido_da_401()
    {
        var cliente = Cliente(_fabrica.EmitirToken(Rol.AdministradorGeneral, restauranteId: Guid.NewGuid()));

        var respuesta = await cliente.GetAsync("/api/r/resto-a/usuarios");

        await AssertProblemaAsync(respuesta, HttpStatusCode.Unauthorized, "No autenticado", "El restaurante no está activo.");
        Assert.Contains("Bearer", respuesta.Headers.WwwAuthenticate.Select(h => h.Scheme));
    }

    [Fact(DisplayName = "002/RF-12: token del restaurante A en la ruta del B da 404")]
    public async Task Token_de_a_en_ruta_de_b_da_404()
    {
        Sembrar(_b.Id, "beto", Rol.Cajero);

        var respuesta = await AdminDe(_a).GetAsync("/api/r/resto-b/usuarios");

        await AssertProblemaAsync(respuesta, HttpStatusCode.NotFound, "Restaurante no disponible");
    }

    [Fact(DisplayName = "002/RF-12: token del restaurante A con su slug en mayúsculas sigue siendo válido")]
    public async Task Token_de_a_con_slug_en_mayusculas_es_valido()
    {
        Sembrar(_a.Id, "ana", Rol.Cajero);

        var nombres = await NombresAsync(await AdminDe(_a).GetAsync("/api/r/Resto-A/usuarios"));

        Assert.Equal(["admin", "ana"], nombres);
    }

    [Fact(DisplayName = "002/RF-12: A no ve ni modifica un usuario de B por id")]
    public async Task A_no_ve_ni_modifica_usuario_de_b()
    {
        var deB = Sembrar(_b.Id, "beto", Rol.Cajero);
        var cliente = AdminDe(_a);

        var estado = await cliente.PutAsJsonAsync($"/api/r/resto-a/usuarios/{deB.Id}/estado", new { activo = false });
        // Edición de rol y sucursales (004/RF-04; sustituye a PUT /{id}/rol).
        var rol = await cliente.PutAsJsonAsync(
            $"/api/r/resto-a/usuarios/{deB.Id}", new { rol = "cocinero", sucursales = new[] { "centro" } });

        Assert.Equal(HttpStatusCode.NotFound, estado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, rol.StatusCode);
        Assert.True(deB.Activo);
        Assert.Equal(Rol.Cajero, deB.Rol);
    }

    [Fact(DisplayName = "002/RF-13: el administrador general crea usuarios en su restaurante aunque el nombre exista en otro")]
    public async Task Crea_usuario_con_nombre_existente_en_otro_restaurante()
    {
        Sembrar(_b.Id, "maria", Rol.Cajero);
        // Desde 004/RF-02 el cocinero lleva al menos una sucursal de su restaurante.
        _fabrica.SembrarSucursal(_a.Id, "centro", "Centro");

        var alta = await AdminDe(_a).PostAsJsonAsync("/api/r/resto-a/usuarios",
            new { nombreUsuario = "maria", contrasena = Contrasena, rol = "cocinero", sucursales = new[] { "centro" } });

        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        Assert.Equal(["admin", "maria"], await NombresAsync(await AdminDe(_a).GetAsync("/api/r/resto-a/usuarios")));
        var deB = await NombresAsync(await AdminDe(_b).GetAsync("/api/r/resto-b/usuarios"));
        Assert.Equal(["admin", "maria"], deB);
        Assert.Equal(2, _fabrica.Usuarios.Usuarios.Count(u => u.NombreUsuario == "maria"));
        Assert.Contains(_fabrica.Usuarios.Usuarios, u => u.NombreUsuario == "maria" && u.RestauranteId == _a.Id);
        Assert.Single(_fabrica.Usuarios.Usuarios, u => u.NombreUsuario == "maria" && u.RestauranteId == _b.Id
            && u.Rol == Rol.Cajero);
    }

    [Fact(DisplayName = "002/RF-12: un token de plataforma en /api/r/{slug}/usuarios recibe 403")]
    public async Task Token_de_plataforma_en_usuarios_da_403()
    {
        var respuesta = await Cliente(_fabrica.EmitirToken(Rol.AdministradorPlataforma))
            .GetAsync("/api/r/resto-a/usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
