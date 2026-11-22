using MediatR;

namespace Restaurante.Application.Features.Restaurantes.ObtenerIdentidad;

/// <summary>Identidad pública (nombre, paleta y logo) del restaurante fijado por el slug de la ruta.</summary>
public sealed record ObtenerIdentidadQuery : IRequest<IdentidadDto>;
