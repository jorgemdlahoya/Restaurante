using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Restaurante.Domain.Restaurantes;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Restaurantes;

[Collection(ColeccionApi.Nombre)]
public sealed class LogoTests : IDisposable
{
    private static readonly byte[] CabeceraPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly FabricaApi _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    private static byte[] Png(int largo)
    {
        var bytes = new byte[largo];
        CabeceraPng.CopyTo(bytes, 0);
        return bytes;
    }

    private HttpClient ClientePlataforma(Rol rol = Rol.AdministradorPlataforma)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FabricaApi.EmitirToken(rol));
        return cliente;
    }

    private static MultipartFormDataContent Formulario(byte[] contenido, string nombre = "logo.png")
    {
        var archivo = new ByteArrayContent(contenido);
        archivo.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { archivo, "logo", nombre } };
    }

    [Theory(DisplayName = "002/RF-05: SVG, archivo de 1 MB + 1 byte o de 3 MB dan 400 en logo")]
    [InlineData("svg", "El logo debe ser PNG, JPG o WebP")]
    [InlineData("1mb", "El logo no puede superar 1 MB")]
    [InlineData("3mb", "El logo no puede superar 1 MB")]
    public async Task Logo_invalido_da_400(string caso, string mensaje)
    {
        var r = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        var contenido = caso switch
        {
            "svg" => System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"),
            "1mb" => Png(1_048_577),
            _ => Png(3 * 1024 * 1024),
        };

        var respuesta = await ClientePlataforma().PutAsync($"/api/plataforma/restaurantes/{r.Id}/logo", Formulario(contenido));

        await AfirmacionesHttp.AssertValidacionAsync(respuesta, "logo", mensaje);
    }

    [Fact(DisplayName = "002/RF-05: subir logo válido responde 204 y queda guardado")]
    public async Task Subir_valido_da_204()
    {
        var r = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");

        var respuesta = await ClientePlataforma().PutAsync($"/api/plataforma/restaurantes/{r.Id}/logo", Formulario(Png(100)));

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Single(_fabrica.Restaurantes.Logos);
    }

    [Fact(DisplayName = "002/RF-05: subir logo sin archivo responde 400 El logo es obligatorio")]
    public async Task Sin_archivo_da_400()
    {
        var r = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        var vacio = new MultipartFormDataContent { { new StringContent("x"), "otro" } };

        var respuesta = await ClientePlataforma().PutAsync($"/api/plataforma/restaurantes/{r.Id}/logo", vacio);

        await AfirmacionesHttp.AssertValidacionAsync(respuesta, "logo", "El logo es obligatorio");
    }

    [Fact(DisplayName = "002/RF-05: subir logo exige administrador_plataforma")]
    public async Task Exige_rol()
    {
        var r = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");

        var respuesta = await ClientePlataforma(Rol.AdministradorGeneral)
            .PutAsync($"/api/plataforma/restaurantes/{r.Id}/logo", Formulario(Png(100)));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact(DisplayName = "002/RF-15: GET logo devuelve la imagen con su tipo y ETag")]
    public async Task Get_logo()
    {
        var r = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        var png = Png(64);
        var etag = Convert.ToHexStringLower(SHA256.HashData(png));
        _fabrica.Restaurantes.AgregarLogo(LogoRestaurante.Crear(r.Id, png, "image/png", etag, DateTimeOffset.UtcNow));

        var respuesta = await _fabrica.CreateClient().GetAsync("/api/r/la-esquina/logo");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("image/png", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal($"\"{etag}\"", respuesta.Headers.ETag?.Tag);
        Assert.Equal("public, no-cache", respuesta.Headers.CacheControl?.ToString());
        Assert.Equal(png, await respuesta.Content.ReadAsByteArrayAsync());
    }

    [Fact(DisplayName = "002/RF-15: con If-None-Match igual responde 304")]
    public async Task Get_logo_304()
    {
        var r = _fabrica.SembrarRestaurante("La Esquina", "la-esquina");
        _fabrica.Restaurantes.AgregarLogo(LogoRestaurante.Crear(r.Id, Png(64), "image/png", "abc123", DateTimeOffset.UtcNow));
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/r/la-esquina/logo");
        peticion.Headers.TryAddWithoutValidation("If-None-Match", "\"abc123\"");

        var respuesta = await _fabrica.CreateClient().SendAsync(peticion);

        Assert.Equal(HttpStatusCode.NotModified, respuesta.StatusCode);
        Assert.Empty(await respuesta.Content.ReadAsByteArrayAsync());
    }

    [Fact(DisplayName = "002/RF-15: sin logo responde 404")]
    public async Task Get_sin_logo_404()
    {
        _fabrica.SembrarRestaurante("La Esquina", "la-esquina");

        var respuesta = await _fabrica.CreateClient().GetAsync("/api/r/la-esquina/logo");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
