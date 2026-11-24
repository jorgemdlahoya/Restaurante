using Restaurante.Domain.Restaurantes;

namespace Restaurante.Application.Features.Restaurantes;

/// <summary>Paleta de colores de un restaurante; cada color es opcional y, si existe, tiene el formato #RRGGBB.</summary>
public sealed record PaletaDto(string? Principal, string? Acento, string? Oscuro, string? Fondo, string? Texto)
{
    public static PaletaDto Desde(Paleta paleta) =>
        new(paleta.Principal, paleta.Acento, paleta.Oscuro, paleta.Fondo, paleta.Texto);

    public Paleta ADominio() => new(Principal, Acento, Oscuro, Fondo, Texto);
}
