using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Dobles;

public sealed class UsuarioRepositoryEnMemoria : IUsuarioRepository
{
    public List<Usuario> Usuarios { get; } = new();

    public int GuardadosLlamados { get; private set; }

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Usuarios.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> ObtenerPorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken ct) =>
        Task.FromResult(Usuarios.FirstOrDefault(u => u.NombreUsuarioNormalizado == nombreNormalizado));

    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Usuario>>(Usuarios.ToList());

    public Task<int> ContarAdministradoresActivosAsync(CancellationToken ct) =>
        Task.FromResult(Usuarios.Count(u => u.Activo && u.Rol == Rol.AdministradorGeneral));

    public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => Task.FromResult(Usuarios.Count > 0);

    public void Agregar(Usuario usuario) => Usuarios.Add(usuario);

    public Task GuardarCambiosAsync(CancellationToken ct)
    {
        GuardadosLlamados++;
        return Task.CompletedTask;
    }
}
