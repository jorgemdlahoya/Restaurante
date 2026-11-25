using Restaurante.Domain.Comun;

namespace Restaurante.Domain.Menu;

public sealed class Categoria : IEntidadDeRestaurante, IEliminable
{
    public const int LongitudMaximaNombre = 60;
    public const int LongitudMaximaNombreCorto = 24;

    // Requerido por EF Core.
    private Categoria()
    {
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
    }

    private Categoria(Guid id, Guid restauranteId, DatosCategoria datos, DateTimeOffset creadaEn)
    {
        Id = id;
        RestauranteId = restauranteId;
        Activa = true;
        CreadaEn = creadaEn;
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
        Aplicar(datos);
    }

    public Guid Id { get; private set; }
    public Guid RestauranteId { get; private set; }
    public string Nombre { get; private set; }
    public string NombreNormalizado { get; private set; }
    public string? NombreCorto { get; private set; }
    public string? NombreCortoNormalizado { get; private set; }
    public int Orden { get; private set; }
    public bool Activa { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; }
    public DateTimeOffset? EliminadaEn { get; private set; }

    public static string Normalizar(string texto) => texto.Trim().ToLowerInvariant();

    public static Categoria Crear(Guid restauranteId, DatosCategoria datos, DateTimeOffset creadaEn)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return new Categoria(Guid.NewGuid(), restauranteId, datos, creadaEn);
    }

    public void Editar(DatosCategoria datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        Aplicar(datos);
    }

    public void Activar() => Activa = true;

    public void Desactivar() => Activa = false;

    public void Eliminar(DateTimeOffset ahora) => EliminadaEn = ahora;

    // Valida todo antes de asignar, para no dejar la entidad a medias si algo falla.
    private void Aplicar(DatosCategoria datos)
    {
        var nombre = datos.Nombre?.Trim() ?? string.Empty;
        if (nombre.Length is 0 || nombre.Length > LongitudMaximaNombre)
        {
            throw new ArgumentException(
                $"El nombre debe tener entre 1 y {LongitudMaximaNombre} caracteres.", nameof(datos));
        }

        var corto = datos.NombreCorto?.Trim();
        if (string.IsNullOrEmpty(corto))
        {
            corto = null;
        }
        else if (corto.Length > LongitudMaximaNombreCorto)
        {
            throw new ArgumentException(
                $"El nombre corto no puede superar {LongitudMaximaNombreCorto} caracteres.", nameof(datos));
        }

        if (datos.Orden < 1)
        {
            throw new ArgumentException("El orden debe ser un entero mayor o igual que 1.", nameof(datos));
        }

        Nombre = nombre;
        NombreNormalizado = Normalizar(nombre);
        NombreCorto = corto;
        NombreCortoNormalizado = corto is null ? null : Normalizar(corto);
        Orden = datos.Orden;
    }
}
