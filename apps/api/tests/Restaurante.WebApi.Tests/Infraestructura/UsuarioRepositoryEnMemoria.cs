using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.WebApi.Tests.Infraestructura;

public sealed class UsuarioRepositoryEnMemoria : IUsuarioRepository
{
    public List<Usuario> Usuarios { get; } = new();

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Usuarios.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> ObtenerPorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken ct) =>
        Task.FromResult(Usuarios.FirstOrDefault(u => u.NombreUsuarioNormalizado == nombreNormalizado));

    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Usuario>>(Usuarios.ToList());

    public Task<int> ContarAdministradoresActivosAsync(CancellationToken ct) =>
        Task.FromResult(Usuarios.Count(u => u.Activo && u.Rol == Rol.Administrador));

    public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => Task.FromResult(Usuarios.Count > 0);

    public void Agregar(Usuario usuario) => Usuarios.Add(usuario);

    public Task GuardarCambiosAsync(CancellationToken ct) => Task.CompletedTask;
}
