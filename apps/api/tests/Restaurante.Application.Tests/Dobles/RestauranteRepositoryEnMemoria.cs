using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Restaurantes;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Application.Tests.Dobles;

/// <summary>Repositorio de restaurantes en memoria. Nombre y slug se comparan sin distinguir mayúsculas.</summary>
public sealed class RestauranteRepositoryEnMemoria : IRestauranteRepository
{
    public List<RestauranteEntidad> Restaurantes { get; } = new();

    public List<LogoRestaurante> Logos { get; } = new();

    public int GuardadosLlamados { get; private set; }

    public Task<RestauranteEntidad?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Restaurantes.FirstOrDefault(r => r.Id == id));

    public Task<RestauranteEntidad?> ObtenerPorSlugAsync(string slug, CancellationToken ct) =>
        Task.FromResult(Restaurantes.FirstOrDefault(r => r.Slug == slug));

    public Task<IReadOnlyList<RestauranteEntidad>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<RestauranteEntidad>>(Restaurantes.OrderBy(r => r.Nombre).ToList());

    public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(Restaurantes.Any(r =>
            r.Id != excluirId && string.Equals(r.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExisteSlugAsync(string slug, CancellationToken ct) =>
        Task.FromResult(Restaurantes.Any(r => r.Slug == slug));

    public void Agregar(RestauranteEntidad restaurante) => Restaurantes.Add(restaurante);

    public Task<LogoRestaurante?> ObtenerLogoAsync(Guid restauranteId, CancellationToken ct) =>
        Task.FromResult(Logos.FirstOrDefault(l => l.RestauranteId == restauranteId));

    public Task<string?> ObtenerEtagLogoAsync(Guid restauranteId, CancellationToken ct) =>
        Task.FromResult(Logos.FirstOrDefault(l => l.RestauranteId == restauranteId)?.ETag);

    public void AgregarLogo(LogoRestaurante logo) => Logos.Add(logo);

    public Task GuardarCambiosAsync(CancellationToken ct)
    {
        GuardadosLlamados++;
        return Task.CompletedTask;
    }
}
