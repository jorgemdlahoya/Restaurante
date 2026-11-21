using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Abstracciones;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Usuario?> ObtenerPorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken ct);

    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct);

    Task<int> ContarAdministradoresActivosAsync(CancellationToken ct);

    Task<bool> ExisteAlgunoAsync(CancellationToken ct);

    void Agregar(Usuario usuario);

    Task GuardarCambiosAsync(CancellationToken ct);
}
