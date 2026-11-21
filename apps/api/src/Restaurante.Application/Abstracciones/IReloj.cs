namespace Restaurante.Application.Abstracciones;

public interface IReloj
{
    DateTimeOffset Ahora { get; }
}
