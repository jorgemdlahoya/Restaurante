using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Abstracciones;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Lectura de sesión (004/RF-13): por clave primaria, sin filtro de restaurante, con sucursales y sin
    /// seguimiento. La usa el middleware antes de que exista el contexto de restaurante.
    /// </summary>
    Task<Usuario?> ObtenerParaSesionAsync(Guid id, CancellationToken ct);

    Task<Usuario?> ObtenerPorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken ct);

    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct);

    Task<int> ContarAdministradoresActivosAsync(CancellationToken ct);

    Task<bool> ExisteAlgunoAsync(CancellationToken ct);

    void Agregar(Usuario usuario);

    Task GuardarCambiosAsync(CancellationToken ct);
}
