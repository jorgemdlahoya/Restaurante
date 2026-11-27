using Restaurante.Domain.Menu;

namespace Restaurante.Application.Abstracciones;

/// <summary>Categorías del restaurante del contexto, sin eliminadas (filtro global).</summary>
public interface ICategoriaRepository
{
    Task<Categoria?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Página (base 1) ordenada por orden y nombre, con el total de categorías.</summary>
    Task<(IReadOnlyList<Categoria> Elementos, int Total)> ListarAsync(int pagina, int tamano, CancellationToken ct);

    Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluirId, CancellationToken ct);

    Task<bool> ExisteNombreCortoAsync(string nombreCortoNormalizado, Guid? excluirId, CancellationToken ct);

    void Agregar(Categoria categoria);

    /// <summary>Guarda; una violación de unicidad se traduce a <c>ConflictoCamposException</c>.</summary>
    Task GuardarCambiosAsync(CancellationToken ct);
}
