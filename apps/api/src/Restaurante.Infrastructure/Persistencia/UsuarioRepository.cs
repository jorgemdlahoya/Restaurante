using Microsoft.EntityFrameworkCore;
using Npgsql;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class UsuarioRepository(RestauranteDbContext contexto) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        contexto.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObtenerPorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken ct) =>
        contexto.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuarioNormalizado == nombreNormalizado, ct);

    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct) =>
        await contexto.Usuarios.AsNoTracking().OrderBy(u => u.CreadoEn).ThenBy(u => u.NombreUsuarioNormalizado).ToListAsync(ct);

    public Task<int> ContarAdministradoresActivosAsync(CancellationToken ct) =>
        contexto.Usuarios.CountAsync(u => u.Rol == Rol.Administrador && u.Activo, ct);

    public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => contexto.Usuarios.AnyAsync(ct);

    public void Agregar(Usuario usuario) => contexto.Usuarios.Add(usuario);

    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictoException("Ya existe un usuario con ese nombre");
        }
    }
}
