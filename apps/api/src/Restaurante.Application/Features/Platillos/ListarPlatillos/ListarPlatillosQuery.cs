using MediatR;

namespace Restaurante.Application.Features.Platillos.ListarPlatillos;

public sealed record ListarPlatillosQuery(Guid CategoriaId, int Pagina, int Tamano) : IRequest<PaginaPlatillosDto>;
