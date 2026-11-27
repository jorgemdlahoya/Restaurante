namespace Restaurante.Application.Features.Platillos;

public sealed record PaginaPlatillosDto(IReadOnlyList<PlatilloDto> Elementos, int Total, int Pagina, int Tamano);
