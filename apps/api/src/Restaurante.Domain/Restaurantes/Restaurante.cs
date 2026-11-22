using System.Text.RegularExpressions;

namespace Restaurante.Domain.Restaurantes;

public sealed partial class Restaurante
{
    public const string PatronSlug = "^[a-z0-9-]{3,40}$";
    public const int LongitudMaximaNombre = 80;

    // Requerido por EF Core.
    private Restaurante()
    {
        Nombre = string.Empty;
        Slug = string.Empty;
        Paleta = Paleta.Vacia;
    }

    private Restaurante(Guid id, string nombre, string slug, Paleta paleta, DateTimeOffset creadoEn)
    {
        Id = id;
        Nombre = nombre;
        Slug = slug;
        Paleta = paleta;
        Activo = true;
        CreadoEn = creadoEn;
    }

    public Guid Id { get; private set; }
    public string Nombre { get; private set; }
    public string Slug { get; private set; }
    public bool Activo { get; private set; }
    public Paleta Paleta { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }

    public static Restaurante Crear(string nombre, string slug, Paleta paleta, DateTimeOffset creadoEn)
    {
        ArgumentNullException.ThrowIfNull(paleta);
        if (slug is null || !PatronSlugRegex().IsMatch(slug))
        {
            throw new ArgumentException("El slug debe cumplir ^[a-z0-9-]{3,40}$.", nameof(slug));
        }

        return new Restaurante(Guid.NewGuid(), NombreValido(nombre), slug, paleta, creadoEn);
    }

    public void CambiarNombre(string nombre) => Nombre = NombreValido(nombre);

    public void CambiarPaleta(Paleta paleta)
    {
        ArgumentNullException.ThrowIfNull(paleta);
        Paleta = paleta;
    }

    public void Activar() => Activo = true;

    public void Desactivar() => Activo = false;

    private static string NombreValido(string nombre)
    {
        var recortado = nombre?.Trim() ?? string.Empty;
        if (recortado.Length is 0 or > LongitudMaximaNombre)
        {
            throw new ArgumentException(
                $"El nombre debe tener entre 1 y {LongitudMaximaNombre} caracteres.", nameof(nombre));
        }

        return recortado;
    }

    [GeneratedRegex(PatronSlug)]
    private static partial Regex PatronSlugRegex();
}
