using System.Text.RegularExpressions;
using Restaurante.Domain.Comun;

namespace Restaurante.Domain.Menu;

public sealed class Variante : IEntidadDeRestaurante, IEliminable
{
    public const int LongitudMaximaNombre = 24;
    public const string NombreRegular = "Regular";
    public const string PatronSku = @"^[A-Za-z0-9_-]{1,40}\z";

    private static readonly Regex ExpresionSku = new(PatronSku, RegexOptions.CultureInvariant);

    // Requerido por EF Core.
    private Variante()
    {
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
    }

    private Variante(Guid id, Guid restauranteId, Guid platilloId, DatosVariante datos, DateTimeOffset creadaEn)
    {
        Id = id;
        RestauranteId = restauranteId;
        PlatilloId = platilloId;
        Activa = true;
        CreadaEn = creadaEn;
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
        Aplicar(datos);
    }

    public Guid Id { get; private set; }
    public Guid RestauranteId { get; private set; }
    public Guid PlatilloId { get; private set; }
    public string Nombre { get; private set; }
    public string NombreNormalizado { get; private set; }
    public decimal Precio { get; private set; }
    public string? Sku { get; private set; }
    public string? SkuNormalizado { get; private set; }
    public bool Activa { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; }
    public DateTimeOffset? EliminadaEn { get; private set; }

    public static string NormalizarNombre(string nombre) => nombre.Trim().ToLowerInvariant();

    public static string NormalizarSku(string sku) => sku.Trim().ToUpperInvariant();

    public static Variante Crear(Guid restauranteId, Guid platilloId, DatosVariante datos, DateTimeOffset creadaEn)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return new Variante(Guid.NewGuid(), restauranteId, platilloId, datos, creadaEn);
    }

    public static Variante Regular(Guid restauranteId, Guid platilloId, decimal precio, DateTimeOffset creadaEn) =>
        Crear(restauranteId, platilloId, new DatosVariante(NombreRegular, precio, null), creadaEn);

    public void Editar(DatosVariante datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        Aplicar(datos);
    }

    public void Activar() => Activa = true;

    public void Desactivar() => Activa = false;

    public void Eliminar(DateTimeOffset ahora) => EliminadaEn = ahora;

    // Valida todo antes de asignar, para no dejar la entidad a medias si algo falla.
    private void Aplicar(DatosVariante datos)
    {
        var nombre = datos.Nombre?.Trim() ?? string.Empty;
        if (nombre.Length is 0 || nombre.Length > LongitudMaximaNombre)
        {
            throw new ArgumentException(
                $"El nombre debe tener entre 1 y {LongitudMaximaNombre} caracteres.", nameof(datos));
        }

        var errores = PrecioVariante.Errores(datos.Precio);
        if (errores.Count > 0)
        {
            throw new ArgumentException(string.Join("; ", errores), nameof(datos));
        }

        var sku = datos.Sku?.Trim();
        if (string.IsNullOrEmpty(sku))
        {
            sku = null;
        }
        else if (!ExpresionSku.IsMatch(sku))
        {
            throw new ArgumentException(
                "El SKU sólo admite letras, números, guiones y guion bajo (hasta 40).", nameof(datos));
        }

        Nombre = nombre;
        NombreNormalizado = NormalizarNombre(nombre);
        Precio = datos.Precio;
        Sku = sku;
        SkuNormalizado = sku is null ? null : NormalizarSku(sku);
    }
}
