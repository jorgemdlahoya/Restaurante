using Restaurante.Application.Abstracciones;

namespace Restaurante.Infrastructure;

public sealed class Reloj : IReloj
{
    public DateTimeOffset Ahora => DateTimeOffset.UtcNow;
}
