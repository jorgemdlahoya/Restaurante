namespace Restaurante.Application.Features.Restaurantes.ObtenerLogo;

/// <summary>Contenido binario del logo con su tipo MIME y su ETag (hash SHA-256 en hexadecimal).</summary>
public sealed record LogoDto(byte[] Contenido, string Tipo, string ETag);
