using System.Net;
using System.Text.Json;

namespace Restaurante.WebApi.Tests.Infraestructura;

/// <summary>Afirmaciones compartidas sobre la forma uniforme de las respuestas de error.</summary>
public static class AfirmacionesHttp
{
    public static async Task AssertValidacionAsync(HttpResponseMessage respuesta, string? campo, string mensaje)
    {
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        Assert.Equal(400, raiz.GetProperty("status").GetInt32());
        Assert.Equal("Datos inválidos", raiz.GetProperty("title").GetString());
        Assert.Equal("Uno o más campos no son válidos.", raiz.GetProperty("detail").GetString());
        var errores = raiz.GetProperty("errors");
        if (campo is not null)
        {
            Assert.Contains(mensaje, errores.GetProperty(campo).EnumerateArray().Select(e => e.GetString()));
        }
        else
        {
            Assert.Contains(errores.EnumerateObject(),
                p => p.Value.EnumerateArray().Any(e => e.GetString() == mensaje));
        }
    }
}
