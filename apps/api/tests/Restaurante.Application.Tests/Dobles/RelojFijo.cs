using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Tests.Dobles;

public sealed class RelojFijo(DateTimeOffset ahora) : IReloj
{
    public RelojFijo() : this(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero))
    {
    }

    public DateTimeOffset Ahora { get; set; } = ahora;
}
