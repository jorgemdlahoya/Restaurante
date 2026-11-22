using System.Security.Cryptography;
using Restaurante.Application.Features.Restaurantes.SubirLogo;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Restaurantes;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Application.Tests.Features.Restaurantes;

public class SubirLogoTests
{
    public static IEnumerable<object?[]> Casos()
    {
        yield return [new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0 }, "image/png"];
        yield return [new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0 }, "image/jpeg"];
        yield return [new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 1, 2, 3, 4, (byte)'W', (byte)'E', (byte)'B', (byte)'P' }, "image/webp"];
        yield return [System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"), null];
        yield return [System.Text.Encoding.UTF8.GetBytes("hola mundo"), null];
        yield return [Array.Empty<byte>(), null];
        yield return [new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 1, 2, 3, 4, (byte)'W', (byte)'A', (byte)'V', (byte)'E' }, null];
    }

    [Theory(DisplayName = "002/RF-05: detecta PNG, JPG y WebP por firma y rechaza SVG y texto")]
    [MemberData(nameof(Casos))]
    public void DetectaPorFirma(byte[] bytes, string? esperado) =>
        Assert.Equal(esperado, DetectorTipoImagen.Detectar(bytes));

    [Fact(DisplayName = "002/RF-05: subir un PNG válido guarda contenido, tipo y etag SHA-256")]
    public async Task GuardaLogo()
    {
        var repo = new RestauranteRepositoryEnMemoria();
        var reloj = new RelojFijo();
        var r = RestauranteEntidad.Crear("La Esquina", "la-esquina", Paleta.Vacia, reloj.Ahora);
        repo.Agregar(r);
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

        await new SubirLogoHandler(repo, reloj).Handle(new SubirLogoCommand(r.Id, png), CancellationToken.None);

        var logo = Assert.Single(repo.Logos);
        Assert.Equal(png, logo.Contenido);
        Assert.Equal("image/png", logo.Tipo);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(png)), logo.ETag);
        Assert.Equal(1, repo.GuardadosLlamados);
    }
}
