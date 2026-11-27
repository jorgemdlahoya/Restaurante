using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Tests.Dobles;

/// <summary>
/// Repositorio en memoria que emula el filtro global de EF: sólo ve las categorías del restaurante del
/// contexto y no eliminadas. <see cref="Categorias"/> expone el almacén completo.
/// </summary>
public sealed class CategoriaRepositoryEnMemoria(IContextoRestaurante? contexto = null) : ICategoriaRepository
{
    private readonly Guid? _restauranteId = contexto?.RestauranteId;

    public List<Categoria> Categorias { get; } = new();

    public int GuardadosLlamados { get; private set; }

    private IEnumerable<Categoria> Visibles =>
        Categorias.Where(c => c.RestauranteId == _restauranteId && c.EliminadaEn is null);

    public Task<Categoria?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(c => c.Id == id));

    public Task<(IReadOnlyList<Categoria> Elementos, int Total)> ListarAsync(int pagina, int tamano, CancellationToken ct)
    {
        var todas = Visibles.OrderBy(c => c.Orden).ThenBy(c => c.NombreNormalizado).ThenBy(c => c.Id).ToList();
        var desplazamiento = ((long)pagina - 1) * tamano;
        IReadOnlyList<Categoria> elementos = desplazamiento >= todas.Count
            ? []
            : todas.Skip((int)desplazamiento).Take(tamano).ToList();
        return Task.FromResult((elementos, todas.Count));
    }

    public Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(Visibles.Any(c => c.Id != excluirId && c.NombreNormalizado == nombreNormalizado));

    public Task<bool> ExisteNombreCortoAsync(string nombreCortoNormalizado, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(Visibles.Any(c => c.Id != excluirId && c.NombreCortoNormalizado == nombreCortoNormalizado));

    public void Agregar(Categoria categoria) => Categorias.Add(categoria);

    public Task GuardarCambiosAsync(CancellationToken ct)
    {
        GuardadosLlamados++;
        return Task.CompletedTask;
    }
}
