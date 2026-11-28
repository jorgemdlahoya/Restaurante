using Microsoft.EntityFrameworkCore;
using Npgsql;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Platillos;
using Restaurante.Domain.Menu;

namespace Restaurante.Infrastructure.Persistencia;

/// <summary>El filtro global limita todo al restaurante del contexto y excluye los platillos eliminados.</summary>
public sealed class PlatilloRepository(RestauranteDbContext contexto) : IPlatilloRepository
{
    private const string IndiceNombreCorto = "ix_platillos_restaurante_nombre_corto";

    public Task<Platillo?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        contexto.Platillos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Platillo> Elementos, int Total)> ListarPorCategoriaAsync(
        Guid categoriaId, int pagina, int tamano, CancellationToken ct)
    {
        var total = await contexto.Platillos.CountAsync(p => p.CategoriaId == categoriaId, ct);
        // El desplazamiento se calcula en long: con una página enorme (int) desbordaría a un OFFSET negativo.
        var desplazamiento = ((long)pagina - 1) * tamano;
        if (desplazamiento >= total)
        {
            return ([], total);
        }

        var elementos = await contexto.Platillos.AsNoTracking()
            .Where(p => p.CategoriaId == categoriaId)
            .OrderBy(p => p.Orden).ThenBy(p => p.Nombre).ThenBy(p => p.Id)
            .Skip((int)desplazamiento).Take(tamano)
            .ToListAsync(ct);
        return (elementos, total);
    }

    public Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluirId, CancellationToken ct) =>
        contexto.Platillos.AnyAsync(
            p => p.NombreNormalizado == nombreNormalizado && (excluirId == null || p.Id != excluirId), ct);

    public Task<bool> ExisteNombreCortoAsync(string nombreCortoNormalizado, Guid? excluirId, CancellationToken ct) =>
        contexto.Platillos.AnyAsync(
            p => p.NombreCortoNormalizado == nombreCortoNormalizado && (excluirId == null || p.Id != excluirId), ct);

    public Task<int> ContarPorCategoriaAsync(Guid categoriaId, CancellationToken ct) =>
        contexto.Platillos.CountAsync(p => p.CategoriaId == categoriaId, ct);

    public void Agregar(Platillo platillo) => contexto.Platillos.Add(platillo);

    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            var errores = pg.ConstraintName == IndiceNombreCorto
                ? new Dictionary<string, string[]> { ["nombreCorto"] = [MensajesPlatillo.NombreCortoRepetido] }
                : new Dictionary<string, string[]> { ["nombre"] = [MensajesPlatillo.NombreRepetido] };
            throw new ConflictoCamposException(errores);
        }
    }
}
