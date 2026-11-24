using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Tests.Dobles;

/// <summary>
/// Repositorio en memoria que emula el filtro global de EF: nombre, slug y prefijo sólo se buscan entre las
/// sucursales del restaurante del contexto; WhatsApp y phone_number_id, en toda la plataforma.
/// <see cref="Sucursales"/> y <see cref="Horarios"/> exponen el almacén completo.
/// </summary>
public sealed class SucursalRepositoryEnMemoria(IContextoRestaurante? contexto = null) : ISucursalRepository
{
    private readonly Guid? _restauranteId = contexto?.RestauranteId;

    public List<Sucursal> Sucursales { get; } = new();

    public List<HorarioDia> Horarios { get; } = new();

    public int GuardadosLlamados { get; private set; }

    private IEnumerable<Sucursal> Visibles => Sucursales.Where(s => s.RestauranteId == _restauranteId);

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
        Task.FromResult(Sucursales.Any(s => s.Id != excluirId && s.WhatsApp == whatsApp));

    public Task<bool> ExistePhoneNumberIdAsync(string phoneNumberId, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(Sucursales.Any(s => s.Id != excluirId && s.PhoneNumberId == phoneNumberId));

    public void Agregar(Sucursal sucursal, IReadOnlyList<HorarioDia> horario)
    {
        Sucursales.Add(sucursal);
        Horarios.AddRange(horario);
    }

    public Task<IReadOnlyList<HorarioDia>> ObtenerHorarioAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<HorarioDia>>(Horarios.OrderBy(h => h.Dia).ToList());

    public Task GuardarCambiosAsync(CancellationToken ct)
    {
        GuardadosLlamados++;
        return Task.CompletedTask;
    }
}
