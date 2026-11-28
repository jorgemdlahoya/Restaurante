using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Tests.Dobles;

/// <summary>
/// Repositorio en memoria que emula el filtro global de EF: sólo ve los platillos del restaurante del
/// contexto y no eliminados. <see cref="Platillos"/> expone el almacén completo.
/// </summary>
public sealed class PlatilloRepositoryEnMemoria(IContextoRestaurante? contexto = null) : IPlatilloRepository
{
    private readonly Guid? _restauranteId = contexto?.RestauranteId;

    public List<Platillo> Platillos { get; } = new();

    public int GuardadosLlamados { get; private set; }

    private IEnumerable<Platillo> Visibles =>
        Platillos.Where(p => p.RestauranteId == _restauranteId && p.EliminadaEn is null);

    public Task<Platillo?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(p => p.Id == id));

    public Task<(IReadOnlyList<Platillo> Elementos, int Total)> ListarPorCategoriaAsync(
        Guid categoriaId, int pagina, int tamano, CancellationToken ct)
    {
        var todos = Visibles.Where(p => p.CategoriaId == categoriaId)
            .OrderBy(p => p.Orden).ThenBy(p => p.Nombre, StringComparer.Ordinal).ThenBy(p => p.Id).ToList();
        var desplazamiento = ((long)pagina - 1) * tamano;
        IReadOnlyList<Platillo> elementos = desplazamiento >= todos.Count
            ? []
            : todos.Skip((int)desplazamiento).Take(tamano).ToList();
        return Task.FromResult((elementos, todos.Count));
    }

    public Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(Visibles.Any(p => p.Id != excluirId && p.NombreNormalizado == nombreNormalizado));

    public Task<bool> ExisteNombreCortoAsync(string nombreCortoNormalizado, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(Visibles.Any(p => p.Id != excluirId && p.NombreCortoNormalizado == nombreCortoNormalizado));

    public Task<int> ContarPorCategoriaAsync(Guid categoriaId, CancellationToken ct) =>
        Task.FromResult(Visibles.Count(p => p.CategoriaId == categoriaId));

    public void Agregar(Platillo platillo) => Platillos.Add(platillo);

    public Task GuardarCambiosAsync(CancellationToken ct)
    {
        GuardadosLlamados++;
        return Task.CompletedTask;
    }
}
