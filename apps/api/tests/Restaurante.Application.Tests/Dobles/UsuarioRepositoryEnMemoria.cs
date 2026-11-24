using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Dobles;

/// <summary>
/// Repositorio en memoria que emula el filtro global de EF: las consultas sólo ven los usuarios cuyo
/// <c>RestauranteId</c> coincide con el del contexto (sin contexto, el ámbito de plataforma).
/// <see cref="Usuarios"/> expone el almacén completo, sin filtrar.
/// </summary>
public sealed class UsuarioRepositoryEnMemoria(IContextoRestaurante? contexto = null) : IUsuarioRepository
{
    private readonly Guid? _restauranteId = contexto?.RestauranteId;

    public List<Usuario> Usuarios { get; } = new();

    public int GuardadosLlamados { get; private set; }

    private IEnumerable<Usuario> Visibles => Usuarios.Where(u => u.RestauranteId == _restauranteId);

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> ObtenerPorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(u => u.NombreUsuarioNormalizado == nombreNormalizado));

    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Usuario>>(Visibles.ToList());

    public Task<int> ContarAdministradoresActivosAsync(CancellationToken ct) =>
        Task.FromResult(Visibles.Count(u => u.Activo && u.Rol == Rol.AdministradorGeneral));

    public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => Task.FromResult(Visibles.Any());

    public void Agregar(Usuario usuario) => Usuarios.Add(usuario);

    public Task GuardarCambiosAsync(CancellationToken ct)
    {
        GuardadosLlamados++;
        return Task.CompletedTask;
    }
}
