namespace Restaurante.Domain.Restaurantes;

public sealed class LogoRestaurante
{
    // Requerido por EF Core.
    private LogoRestaurante()
    {
        Contenido = [];
        Tipo = string.Empty;
        ETag = string.Empty;
    }

    private LogoRestaurante(Guid restauranteId, byte[] contenido, string tipo, string etag, DateTimeOffset ahora)
    {
        RestauranteId = restauranteId;
        Contenido = contenido;
        Tipo = tipo;
        ETag = etag;
        ActualizadoEn = ahora;
    }

    public Guid RestauranteId { get; private set; }
    public byte[] Contenido { get; private set; }
    public string Tipo { get; private set; }
    public string ETag { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    public static LogoRestaurante Crear(Guid restauranteId, byte[] contenido, string tipo, string etag, DateTimeOffset ahora) =>
        new(restauranteId, contenido, tipo, etag, ahora);

    public void Reemplazar(byte[] contenido, string tipo, string etag, DateTimeOffset ahora)
    {
        Contenido = contenido;
        Tipo = tipo;
        ETag = etag;
        ActualizadoEn = ahora;
    }
}
