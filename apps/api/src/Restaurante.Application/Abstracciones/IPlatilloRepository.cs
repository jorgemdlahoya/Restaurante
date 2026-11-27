using Restaurante.Domain.Menu;

namespace Restaurante.Application.Abstracciones;

/// <summary>Platillos del restaurante del contexto, sin eliminados (filtro global).</summary>
public interface IPlatilloRepository
{
    Task<Platillo?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Página (base 1) de una categoría ordenada por orden y nombre, con el total de sus platillos.</summary>
    Task<(IReadOnlyList<Platillo> Elementos, int Total)> ListarPorCategoriaAsync(
        Guid categoriaId, int pagina, int tamano, CancellationToken ct);

    Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluirId, CancellationToken ct);

    Task<bool> ExisteNombreCortoAsync(string nombreCortoNormalizado, Guid? excluirId, CancellationToken ct);

    /// <summary>Platillos no eliminados de la categoría, activos e inactivos.</summary>
    Task<int> ContarPorCategoriaAsync(Guid categoriaId, CancellationToken ct);

    void Agregar(Platillo platillo);

    /// <summary>Guarda; una violación de unicidad se traduce a <c>ConflictoCamposException</c>.</summary>
    Task GuardarCambiosAsync(CancellationToken ct);
}
