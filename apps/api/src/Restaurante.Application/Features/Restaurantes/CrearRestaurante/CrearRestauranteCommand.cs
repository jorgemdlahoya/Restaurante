using MediatR;

namespace Restaurante.Application.Features.Restaurantes.CrearRestaurante;

public sealed record CrearRestauranteCommand(
    string Nombre,
    string Slug,
    PaletaDto? Paleta,
    AdministradorInicialDto Administrador) : IRequest<RestauranteDto>;
