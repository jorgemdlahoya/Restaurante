namespace Restaurante.Application.Abstracciones;

/// <summary>
/// Sucursal en cuyo ámbito se atiende la petición. <c>null</c> significa que la ruta no es de una
/// sucursal: no se ve ninguna entidad de sucursal (horario, cierres).
/// </summary>
public interface IContextoSucursal
{
    Guid? SucursalId { get; }
}
