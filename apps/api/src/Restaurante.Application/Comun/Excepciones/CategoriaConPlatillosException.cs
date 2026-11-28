namespace Restaurante.Application.Comun.Excepciones;

/// <summary>La categoría no se puede eliminar porque aún tiene platillos (activos o inactivos, no eliminados).</summary>
public sealed class CategoriaConPlatillosException(int cantidad)
    : Exception(cantidad == 1 ? "Esta categoría tiene 1 platillo" : $"Esta categoría tiene {cantidad} platillos")
{
    public int Cantidad { get; } = cantidad;
}
