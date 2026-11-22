using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Application.Features.Restaurantes;

/// <summary>Representación pública de un restaurante en la API de plataforma.</summary>
public sealed record RestauranteDto(Guid Id, string Nombre, string Slug, bool Activo, PaletaDto Paleta)
{
    public static RestauranteDto Desde(RestauranteEntidad restaurante) =>
        new(restaurante.Id, restaurante.Nombre, restaurante.Slug, restaurante.Activo, PaletaDto.Desde(restaurante.Paleta));
}
