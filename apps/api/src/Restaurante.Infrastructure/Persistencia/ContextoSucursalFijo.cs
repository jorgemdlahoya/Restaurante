using Restaurante.Application.Abstracciones;

namespace Restaurante.Infrastructure.Persistencia;

/// <summary>Contexto de sucursal con un valor fijo (fábrica de diseño, pruebas y registro provisional).</summary>
public sealed class ContextoSucursalFijo(Guid? sucursalId) : IContextoSucursal
{
    public Guid? SucursalId { get; } = sucursalId;
}
