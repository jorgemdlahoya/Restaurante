using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Tests.Dobles;

/// <summary>Contexto de sucursal fijo para las pruebas; <c>null</c> significa que la ruta no es de una sucursal.</summary>
public sealed class ContextoSucursalFalso(Guid? sucursalId) : IContextoSucursal
{
    public Guid? SucursalId { get; } = sucursalId;
}
