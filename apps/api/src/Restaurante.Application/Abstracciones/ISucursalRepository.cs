using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Abstracciones;

/// <summary>Sucursales del restaurante del contexto (filtro global); el horario, de la sucursal del contexto.</summary>
public interface ISucursalRepository
{
    Task<Sucursal?> ObtenerPorIdAsync(Guid id, CancellationToken ct);
    Task<Sucursal?> ObtenerPorSlugAsync(string slug, CancellationToken ct);

    /// <summary>Ordenadas por nombre.</summary>
    Task<IReadOnlyList<Sucursal>> ListarAsync(CancellationToken ct);

    /// <summary>En el restaurante del contexto.</summary>
    Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct);

    /// <summary>En el restaurante del contexto.</summary>
    Task<bool> ExisteSlugAsync(string slug, CancellationToken ct);

    /// <summary>En el restaurante del contexto.</summary>
    Task<bool> ExistePrefijoAsync(string prefijo, Guid? excluirId, CancellationToken ct);

    /// <summary>En toda la plataforma.</summary>
    Task<bool> ExisteWhatsAppAsync(string whatsApp, Guid? excluirId, CancellationToken ct);

    /// <summary>En toda la plataforma.</summary>
    Task<bool> ExistePhoneNumberIdAsync(string phoneNumberId, Guid? excluirId, CancellationToken ct);

    void Agregar(Sucursal sucursal, IReadOnlyList<HorarioDia> horario);

    /// <summary>Horario de la sucursal del contexto, ordenado por día.</summary>
    Task<IReadOnlyList<HorarioDia>> ObtenerHorarioAsync(CancellationToken ct);

    Task GuardarCambiosAsync(CancellationToken ct);
}
