using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Menu;

namespace Restaurante.WebApi.Tests.Infraestructura;

/// <summary>Almacén de categorías compartido por todas las peticiones de una <see cref="FabricaApi"/>, sin filtrar.</summary>
public sealed class AlmacenCategorias
{
    public List<Categoria> Categorias { get; } = new();
}

/// <summary>
/// Repositorio por petición que emula el filtro global de EF: sólo ve las categorías del
/// <see cref="IContextoRestaurante"/> de la petición y no eliminadas. El contexto se lee en cada consulta
/// porque el middleware lo fija después de crear el ámbito.
/// </summary>
public sealed class CategoriaRepositoryEnMemoria(AlmacenCategorias almacen, IContextoRestaurante contexto)
    : ICategoriaRepository
{
    private IEnumerable<Categoria> Visibles =>
        almacen.Categorias.Where(c => c.RestauranteId == contexto.RestauranteId && c.EliminadaEn is null);

    public Task<Categoria?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(c => c.Id == id));

    public Task<(IReadOnlyList<Categoria> Elementos, int Total)> ListarAsync(int pagina, int tamano, CancellationToken ct)
    {
        var todas = Visibles.OrderBy(c => c.Orden).ThenBy(c => c.NombreNormalizado).ThenBy(c => c.Id).ToList();
        IReadOnlyList<Categoria> elementos = todas.Skip((pagina - 1) * tamano).Take(tamano).ToList();
        return Task.FromResult((elementos, todas.Count));
    }

    public Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(Visibles.Any(c => c.Id != excluirId && c.NombreNormalizado == nombreNormalizado));

    public Task<bool> ExisteNombreCortoAsync(string nombreCortoNormalizado, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(Visibles.Any(c => c.Id != excluirId && c.NombreCortoNormalizado == nombreCortoNormalizado));

    public void Agregar(Categoria categoria) => almacen.Categorias.Add(categoria);

    public Task GuardarCambiosAsync(CancellationToken ct) => Task.CompletedTask;
}
