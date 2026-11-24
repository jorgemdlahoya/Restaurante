using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.WebApi.Tests.Infraestructura;

/// <summary>Almacén de usuarios compartido por todas las peticiones de una <see cref="FabricaApi"/>, sin filtrar.</summary>
public sealed class AlmacenUsuarios
{
    public List<Usuario> Usuarios { get; } = new();

    public void Agregar(Usuario usuario) => Usuarios.Add(usuario);
}

/// <summary>
/// Repositorio por petición que emula el filtro global de EF: sólo ve los usuarios cuyo <c>RestauranteId</c>
/// coincide con el <see cref="IContextoRestaurante"/> de la petición (nulo: ámbito de plataforma). El contexto
/// se lee en cada consulta porque el middleware lo fija después de crear el ámbito.
/// </summary>
public sealed class UsuarioRepositoryEnMemoria(AlmacenUsuarios almacen, IContextoRestaurante contexto) : IUsuarioRepository
{
    private IEnumerable<Usuario> Visibles => almacen.Usuarios.Where(u => u.RestauranteId == contexto.RestauranteId);

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> ObtenerPorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(u => u.NombreUsuarioNormalizado == nombreNormalizado));

    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Usuario>>(Visibles.ToList());

    public Task<int> ContarAdministradoresActivosAsync(CancellationToken ct) =>
        Task.FromResult(Visibles.Count(u => u.Activo && u.Rol == Rol.AdministradorGeneral));

    public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => Task.FromResult(Visibles.Any());

    public void Agregar(Usuario usuario) => almacen.Agregar(usuario);

    public Task GuardarCambiosAsync(CancellationToken ct) => Task.CompletedTask;
}
