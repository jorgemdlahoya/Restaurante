using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Errores;

/// <summary>Errores que ocurren fuera de MVC (ruteo, negociación, middleware) también son problem+json en español.</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class ErroresFueraDeMvcTests
{
    private const string SecretoInterno = "secreto-interno-del-middleware-xyz";

    private static async Task<JsonElement> LeerProblemaAsync(
        HttpResponseMessage respuesta, HttpStatusCode estado, string titulo)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement.Clone();
        Assert.Equal((int)estado, raiz.GetProperty("status").GetInt32());
        Assert.Equal(titulo, raiz.GetProperty("title").GetString());
        return raiz;
    }

    [Fact(DisplayName = "001/RF-19: ruta desconocida responde 404 problem+json en español")]
    public async Task Ruta_desconocida_responde_404_problem_json()
    {
        using var fabrica = new FabricaApi();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/no-existe");

        await LeerProblemaAsync(respuesta, HttpStatusCode.NotFound, "Recurso no encontrado");
    }

    [Fact(DisplayName = "001/RF-19: id que no es GUID responde 404 problem+json en español")]
    public async Task Id_que_no_es_guid_responde_404_problem_json()
    {
        using var fabrica = new FabricaApi();
        var restaurante = fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FabricaApi.EmitirToken(Rol.AdministradorGeneral, restauranteId: restaurante.Id));

        var respuesta = await cliente.PutAsync("/api/r/la-esquina/usuarios/no-es-guid/estado",
            new StringContent("{\"activo\":true}", Encoding.UTF8, "application/json"));

        await LeerProblemaAsync(respuesta, HttpStatusCode.NotFound, "Recurso no encontrado");
    }

    [Fact(DisplayName = "001/RF-20: método no permitido responde 405 problem+json en español")]
    public async Task Metodo_no_permitido_responde_405_problem_json()
    {
        using var fabrica = new FabricaApi();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/plataforma/auth/login");

        await LeerProblemaAsync(respuesta, HttpStatusCode.MethodNotAllowed, "Método no permitido");
    }

    [Fact(DisplayName = "001/RF-20: cuerpo text/plain en login responde 415 problem+json en español")]
    public async Task Cuerpo_text_plain_responde_415_problem_json()
    {
        using var fabrica = new FabricaApi();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsync("/api/plataforma/auth/login",
            new StringContent("usuario=admin", Encoding.UTF8, "text/plain"));

        await LeerProblemaAsync(respuesta, HttpStatusCode.UnsupportedMediaType, "Tipo de contenido no admitido");
    }

    [Fact(DisplayName = "001/RF-20: excepción en un middleware responde 500 problem+json con traceId y sin detalle")]
    public async Task Excepcion_en_middleware_responde_500_problem_json()
    {
        using var fabrica = new FabricaApi
        {
            AjustarServicios = servicios => servicios.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
                o.Events = new JwtBearerEvents
                {
                    OnMessageReceived = _ => throw new InvalidOperationException(SecretoInterno),
                }),
        };
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(cuerpo);
        Assert.Equal("Error interno", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
        Assert.False(json.RootElement.TryGetProperty("detail", out _));
        Assert.DoesNotContain(SecretoInterno, cuerpo);
        Assert.DoesNotContain("InvalidOperationException", cuerpo);
        Assert.DoesNotContain("   at ", cuerpo);
    }
}
