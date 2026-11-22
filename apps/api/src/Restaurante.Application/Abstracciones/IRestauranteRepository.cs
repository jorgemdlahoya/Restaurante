using Restaurante.Domain.Restaurantes;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Application.Abstracciones;

public interface IRestauranteRepository
{
    Task<RestauranteEntidad?> ObtenerPorIdAsync(Guid id, CancellationToken ct);
    Task<RestauranteEntidad?> ObtenerPorSlugAsync(string slug, CancellationToken ct);
    Task<IReadOnlyList<RestauranteEntidad>> ListarAsync(CancellationToken ct);
    Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct);
    Task<bool> ExisteSlugAsync(string slug, CancellationToken ct);
    void Agregar(RestauranteEntidad restaurante);
    Task<LogoRestaurante?> ObtenerLogoAsync(Guid restauranteId, CancellationToken ct);
    Task<string?> ObtenerEtagLogoAsync(Guid restauranteId, CancellationToken ct);
    void AgregarLogo(LogoRestaurante logo);
    Task GuardarCambiosAsync(CancellationToken ct);
}
