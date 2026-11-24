namespace Restaurante.Application.Features.Restaurantes.SubirLogo;

/// <summary>Detecta PNG, JPEG y WebP por su firma binaria; cualquier otro contenido (SVG, texto) da <c>null</c>.</summary>
public static class DetectorTipoImagen
{
    private static ReadOnlySpan<byte> FirmaPng => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> FirmaJpeg => [0xFF, 0xD8, 0xFF];

    private static ReadOnlySpan<byte> Riff => "RIFF"u8;

    private static ReadOnlySpan<byte> Webp => "WEBP"u8;

    public static string? Detectar(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(FirmaPng))
        {
            return "image/png";
        }

        if (bytes.StartsWith(FirmaJpeg))
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 12 && bytes[..4].SequenceEqual(Riff) && bytes[8..12].SequenceEqual(Webp))
        {
            return "image/webp";
        }

        return null;
    }
}
