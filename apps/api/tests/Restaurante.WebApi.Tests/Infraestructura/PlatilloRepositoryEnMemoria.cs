using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Menu;

namespace Restaurante.WebApi.Tests.Infraestructura;

/// <summary>Almacén de platillos compartido por todas las peticiones de una <see cref="FabricaApi"/>, sin filtrar.</summary>
public sealed class AlmacenPlatillos
{
    public List<Platillo> Platillos { get; } = new();
}

/// <summary>
/// Repositorio por petición que emula el filtro global de EF: sólo ve los platillos del
/// <see cref="IContextoRestaurante"/> de la petición y no eliminados. El contexto se lee en cada consulta
/// porque el middleware lo fija después de crear el ámbito.
/// </summary>
public sealed class PlatilloRepositoryEnMemoria(AlmacenPlatillos almacen, IContextoRestaurante contexto)
    : IPlatilloRepository
{
    private IEnumerable<Platillo> Visibles =>
        almacen.Platillos.Where(p => p.RestauranteId == contexto.RestauranteId && p.EliminadaEn is null);

    public Task<Platillo?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(p => p.Id == id));

    public Task<(IReadOnlyList<Platillo> Elementos, int Total)> ListarPorCategoriaAsync(
        Guid categoriaId, int pagina, int tamano, CancellationToken ct)
    {
        var todos = Visibles.Where(p => p.CategoriaId == categoriaId)
            .OrderBy(p => p.Orden).ThenBy(p => p.Nombre, StringComparer.Ordinal).ThenBy(p => p.Id).ToList();
        // Misma aritmética segura que el repositorio real: nunca un Skip negativo por desbordamiento.
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

    public void Agregar(Platillo platillo) => almacen.Platillos.Add(platillo);

    public Task GuardarCambiosAsync(CancellationToken ct) => Task.CompletedTask;
}
