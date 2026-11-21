using Restaurante.Application.Abstracciones;

namespace Restaurante.Infrastructure;

/// <summary>Reloj del sistema sobre <see cref="TimeProvider"/>, para poder fijar la hora en las pruebas.</summary>
public sealed class Reloj(TimeProvider proveedor) : IReloj
{
    public DateTimeOffset Ahora => proveedor.GetUtcNow();
}
