using Microsoft.EntityFrameworkCore;
using Npgsql;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Categorias;
using Restaurante.Domain.Menu;

namespace Restaurante.Infrastructure.Persistencia;

/// <summary>El filtro global limita todo al restaurante del contexto y excluye las categorías eliminadas.</summary>
public sealed class CategoriaRepository(RestauranteDbContext contexto) : ICategoriaRepository
{
    private const string IndiceNombreCorto = "ix_categorias_restaurante_nombre_corto";

    public Task<Categoria?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        contexto.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<(IReadOnlyList<Categoria> Elementos, int Total)> ListarAsync(
        int pagina, int tamano, CancellationToken ct)
    {
        var total = await contexto.Categorias.CountAsync(ct);
        // El desplazamiento se calcula en long: con una página enorme (int) desbordaría a un OFFSET negativo.
        var desplazamiento = ((long)pagina - 1) * tamano;
        if (desplazamiento >= total)
        {
            return ([], total);
        }

        var elementos = await contexto.Categorias.AsNoTracking()
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre).ThenBy(c => c.Id)
            .Skip((int)desplazamiento).Take(tamano)
            .ToListAsync(ct);
        return (elementos, total);
    }

    public Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluirId, CancellationToken ct) =>
        contexto.Categorias.AnyAsync(
            c => c.NombreNormalizado == nombreNormalizado && (excluirId == null || c.Id != excluirId), ct);

    public Task<bool> ExisteNombreCortoAsync(string nombreCortoNormalizado, Guid? excluirId, CancellationToken ct) =>
        contexto.Categorias.AnyAsync(
            c => c.NombreCortoNormalizado == nombreCortoNormalizado && (excluirId == null || c.Id != excluirId), ct);

    public void Agregar(Categoria categoria) => contexto.Categorias.Add(categoria);

    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            var errores = pg.ConstraintName == IndiceNombreCorto
                ? new Dictionary<string, string[]> { ["nombreCorto"] = [MensajesCategoria.NombreCortoRepetido] }
                : new Dictionary<string, string[]> { ["nombre"] = [MensajesCategoria.NombreRepetido] };
            throw new ConflictoCamposException(errores);
        }
    }
}
