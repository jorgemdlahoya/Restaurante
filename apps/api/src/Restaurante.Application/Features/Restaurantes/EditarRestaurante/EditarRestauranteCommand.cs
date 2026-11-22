using MediatR;

namespace Restaurante.Application.Features.Restaurantes.EditarRestaurante;

/// <summary>Cambia nombre y paleta. El slug es inmutable; una paleta nula equivale a la paleta vacía.</summary>
public sealed record EditarRestauranteCommand(Guid Id, string Nombre, PaletaDto? Paleta) : IRequest<RestauranteDto>;
