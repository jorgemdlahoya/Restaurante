using Microsoft.EntityFrameworkCore;
using Npgsql;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Infrastructure.Persistencia;

/// <summary>
/// Las consultas pasan por el filtro global: sucursales del restaurante del contexto y horario de la
/// sucursal del contexto. WhatsApp y phone_number_id se comprueban en toda la plataforma.
/// </summary>
public sealed class SucursalRepository(RestauranteDbContext contexto) : ISucursalRepository
{
    public Task<Sucursal?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        contexto.Sucursales.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Sucursal?> ObtenerPorSlugAsync(string slug, CancellationToken ct) =>
        contexto.Sucursales.FirstOrDefaultAsync(s => s.Slug == slug, ct);

    public async Task<IReadOnlyList<Sucursal>> ListarAsync(CancellationToken ct) =>
        await contexto.Sucursales.AsNoTracking().OrderBy(s => s.Nombre).ToListAsync(ct);

    public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct) =>
        contexto.Sucursales.AnyAsync(s => s.Nombre == nombre && (excluirId == null || s.Id != excluirId), ct);

    public Task<bool> ExisteSlugAsync(string slug, CancellationToken ct) =>
        contexto.Sucursales.AnyAsync(s => s.Slug == slug, ct);

    public Task<bool> ExistePrefijoAsync(string prefijo, Guid? excluirId, CancellationToken ct) =>
        contexto.Sucursales.AnyAsync(s => s.Prefijo == prefijo && (excluirId == null || s.Id != excluirId), ct);

    public Task<bool> ExisteWhatsAppAsync(string whatsApp, Guid? excluirId, CancellationToken ct) =>
        contexto.Sucursales.IgnoreQueryFilters()
            .AnyAsync(s => s.WhatsApp == whatsApp && (excluirId == null || s.Id != excluirId), ct);

    public Task<bool> ExistePhoneNumberIdAsync(string phoneNumberId, Guid? excluirId, CancellationToken ct) =>
        contexto.Sucursales.IgnoreQueryFilters()
            .AnyAsync(s => s.PhoneNumberId == phoneNumberId && (excluirId == null || s.Id != excluirId), ct);

    public void Agregar(Sucursal sucursal, IReadOnlyList<HorarioDia> horario)
    {
        contexto.Sucursales.Add(sucursal);
        contexto.Horarios.AddRange(horario);
    }

    public async Task<IReadOnlyList<HorarioDia>> ObtenerHorarioAsync(CancellationToken ct) =>
        await contexto.Horarios.OrderBy(h => h.Dia).ToListAsync(ct);

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
                "ix_sucursales_restaurante_nombre" => "Ya existe una sucursal con ese nombre",
                "ix_sucursales_restaurante_slug" => "Ya existe una sucursal con ese slug",
                "ix_sucursales_restaurante_prefijo" => "Ya existe una sucursal con ese prefijo",
                "ix_sucursales_whatsapp" => "Ya existe una sucursal con ese WhatsApp",
                "ix_sucursales_phone_number_id" => "Ya existe una sucursal con ese phone_number_id",
                _ => "Ya existe una sucursal con esos datos",
            });
        }
    }
}
