using Microsoft.EntityFrameworkCore;
using Npgsql;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Restaurantes;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class RestauranteRepository(RestauranteDbContext contexto) : IRestauranteRepository
{
    public Task<RestauranteEntidad?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        contexto.Restaurantes.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<RestauranteEntidad?> ObtenerPorSlugAsync(string slug, CancellationToken ct) =>
        contexto.Restaurantes.FirstOrDefaultAsync(r => r.Slug == slug, ct);

    public async Task<IReadOnlyList<RestauranteEntidad>> ListarAsync(CancellationToken ct) =>
        await contexto.Restaurantes.AsNoTracking().OrderBy(r => r.Nombre).ToListAsync(ct);

    public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct) =>
        contexto.Restaurantes.AnyAsync(r => r.Nombre == nombre && (excluirId == null || r.Id != excluirId), ct);

    public Task<bool> ExisteSlugAsync(string slug, CancellationToken ct) =>
        contexto.Restaurantes.AnyAsync(r => r.Slug == slug, ct);

    public void Agregar(RestauranteEntidad restaurante) => contexto.Restaurantes.Add(restaurante);

    public Task<LogoRestaurante?> ObtenerLogoAsync(Guid restauranteId, CancellationToken ct) =>
        contexto.Logos.FirstOrDefaultAsync(l => l.RestauranteId == restauranteId, ct);

    public Task<string?> ObtenerEtagLogoAsync(Guid restauranteId, CancellationToken ct) =>
        contexto.Logos.Where(l => l.RestauranteId == restauranteId).Select(l => (string?)l.ETag).FirstOrDefaultAsync(ct);

    public void AgregarLogo(LogoRestaurante logo) => contexto.Logos.Add(logo);

    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            throw new ConflictoException(pg.ConstraintName switch
            {
                "ix_restaurantes_nombre" => "Ya existe un restaurante con ese nombre",
                "ix_restaurantes_slug" => "Ya existe un restaurante con ese slug",
                _ => "Ya existe un usuario con ese nombre",
            });
        }
    }
}
