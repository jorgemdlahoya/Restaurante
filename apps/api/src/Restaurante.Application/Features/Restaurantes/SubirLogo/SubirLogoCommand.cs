using MediatR;

namespace Restaurante.Application.Features.Restaurantes.SubirLogo;

/// <summary>Crea o reemplaza el logo del restaurante. Un arreglo vacío equivale a "sin archivo".</summary>
public sealed record SubirLogoCommand(Guid RestauranteId, byte[] Logo) : IRequest<Unit>;
