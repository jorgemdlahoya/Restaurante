namespace Restaurante.Application.Features.Restaurantes.ObtenerIdentidad;

/// <summary>Identidad pública del restaurante; <c>LogoUrl</c> es <c>null</c> si no tiene logo.</summary>
public sealed record IdentidadDto(string Nombre, string Slug, PaletaDto Paleta, string? LogoUrl);
