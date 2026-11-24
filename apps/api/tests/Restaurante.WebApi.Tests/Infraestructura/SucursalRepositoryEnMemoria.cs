using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;

namespace Restaurante.WebApi.Tests.Infraestructura;

/// <summary>Almacén de sucursales y horarios compartido por todas las peticiones de una <see cref="FabricaApi"/>, sin filtrar.</summary>
public sealed class AlmacenSucursales
{
    public List<Sucursal> Sucursales { get; } = new();

    public List<HorarioDia> Horarios { get; } = new();
}

/// <summary>
/// Repositorio por petición que emula el filtro global de EF: nombre, slug y prefijo sólo se buscan en el
/// restaurante del <see cref="IContextoRestaurante"/> de la petición; WhatsApp y phone_number_id, en toda la
/// plataforma. El contexto se lee en cada consulta porque el middleware lo fija después de crear el ámbito.
/// </summary>
public sealed class SucursalRepositoryEnMemoria(
    AlmacenSucursales almacen, IContextoRestaurante contexto, IContextoSucursal contextoSucursal) : ISucursalRepository
{
    private IEnumerable<Sucursal> Visibles => almacen.Sucursales.Where(s => s.RestauranteId == contexto.RestauranteId);

    public Task<Sucursal?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(s => s.Id == id));

    public Task<Sucursal?> ObtenerPorSlugAsync(string slug, CancellationToken ct) =>
        Task.FromResult(Visibles.FirstOrDefault(s => s.Slug == slug));

    public Task<IReadOnlyList<Sucursal>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Sucursal>>(Visibles.OrderBy(s => s.Nombre).ToList());

    public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(Visibles.Any(s => s.Id != excluirId && s.Nombre == nombre));

    public Task<bool> ExisteSlugAsync(string slug, CancellationToken ct) =>
        Task.FromResult(Visibles.Any(s => s.Slug == slug));

    public Task<bool> ExistePrefijoAsync(string prefijo, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(Visibles.Any(s => s.Id != excluirId && s.Prefijo == prefijo));

    public Task<bool> ExisteWhatsAppAsync(string whatsApp, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(almacen.Sucursales.Any(s => s.Id != excluirId && s.WhatsApp == whatsApp));

    public Task<bool> ExistePhoneNumberIdAsync(string phoneNumberId, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(almacen.Sucursales.Any(s => s.Id != excluirId && s.PhoneNumberId == phoneNumberId));

    public void Agregar(Sucursal sucursal, IReadOnlyList<HorarioDia> horario)
    {
        almacen.Sucursales.Add(sucursal);
        almacen.Horarios.AddRange(horario);
    }

    /// <summary>Emula el filtro global: sólo filas del restaurante y la sucursal del contexto de la petición.</summary>
    public Task<IReadOnlyList<HorarioDia>> ObtenerHorarioAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<HorarioDia>>(almacen.Horarios
            .Where(h => h.RestauranteId == contexto.RestauranteId && h.SucursalId == contextoSucursal.SucursalId)
            .OrderBy(h => h.Dia)
            .ToList());

    public Task GuardarCambiosAsync(CancellationToken ct) => Task.CompletedTask;
}
