using System.Text.RegularExpressions;
using Restaurante.Domain.Comun;

namespace Restaurante.Domain.Sucursales;

/// <summary>Datos editables de una sucursal (todo menos el slug).</summary>
public sealed record DatosSucursal(
    string Nombre,
    string Direccion,
    string Telefono,
    string WhatsApp,
    string? PhoneNumberId,
    string Prefijo,
    string ZonaHoraria);

public sealed partial class Sucursal : IEntidadDeRestaurante
{
    public const string PatronSlug = @"^[a-z0-9-]{3,40}\z";
    public const int LongitudMaximaNombre = 80;
    public const int LongitudMaximaDireccion = 200;

    // Requerido por EF Core.
    private Sucursal()
    {
        Nombre = string.Empty;
        Direccion = string.Empty;
        Telefono = string.Empty;
        Slug = string.Empty;
        WhatsApp = string.Empty;
        Prefijo = string.Empty;
        ZonaHoraria = ZonasHorarias.PorDefecto;
    }

    private Sucursal(Guid id, Guid restauranteId, string slug, DatosSucursal datos, DateTimeOffset creadaEn)
    {
        Id = id;
        RestauranteId = restauranteId;
        Slug = slug;
        Activa = true;
        CreadaEn = creadaEn;
        Nombre = string.Empty;
        Direccion = string.Empty;
        Telefono = string.Empty;
        WhatsApp = string.Empty;
        Prefijo = string.Empty;
        ZonaHoraria = ZonasHorarias.PorDefecto;
        Aplicar(datos);
    }

    public Guid Id { get; private set; }
    public Guid RestauranteId { get; private set; }
    public string Nombre { get; private set; }
    public string Direccion { get; private set; }
    public string Telefono { get; private set; }
    public string Slug { get; private set; }
    public string WhatsApp { get; private set; }
    public string? PhoneNumberId { get; private set; }
    public string Prefijo { get; private set; }
    public string ZonaHoraria { get; private set; }
    public bool Activa { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; }

    public static Sucursal Crear(Guid restauranteId, string slug, DatosSucursal datos, DateTimeOffset creadaEn)
    {
        ArgumentNullException.ThrowIfNull(datos);
        if (slug is null || !PatronSlugRegex().IsMatch(slug))
        {
            throw new ArgumentException(@"El slug debe cumplir ^[a-z0-9-]{3,40}\z.", nameof(slug));
        }

        return new Sucursal(Guid.NewGuid(), restauranteId, slug, datos, creadaEn);
    }

    public void Editar(DatosSucursal datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        Aplicar(datos);
    }

    public void Activar() => Activa = true;

    public void Desactivar() => Activa = false;

    // Valida todo antes de asignar, para no dejar la entidad a medias si algo falla.
    private void Aplicar(DatosSucursal datos)
    {
        var nombre = TextoAcotado(datos.Nombre, LongitudMaximaNombre, "nombre");
        var direccion = TextoAcotado(datos.Direccion, LongitudMaximaDireccion, "dirección");
        var telefono = TelefonoMexico.Normalizar(datos.Telefono)
            ?? throw new ArgumentException("El teléfono debe cumplir +52 seguido de 10 dígitos.", nameof(datos));
        var whatsApp = TelefonoMexico.Normalizar(datos.WhatsApp)
            ?? throw new ArgumentException("El WhatsApp debe cumplir +52 seguido de 10 dígitos.", nameof(datos));
        if (datos.PhoneNumberId is not null && !PatronPhoneNumberIdRegex().IsMatch(datos.PhoneNumberId))
        {
            throw new ArgumentException(@"El phone_number_id debe cumplir ^[0-9]{1,30}\z.", nameof(datos));
        }

        if (datos.Prefijo is null || !PatronPrefijoRegex().IsMatch(datos.Prefijo))
        {
            throw new ArgumentException(@"El prefijo debe cumplir ^[A-Z]{1,3}\z.", nameof(datos));
        }

        if (!ZonasHorarias.EsAdmitida(datos.ZonaHoraria))
        {
            throw new ArgumentException("La zona horaria no está admitida.", nameof(datos));
        }

        Nombre = nombre;
        Direccion = direccion;
        Telefono = telefono;
        WhatsApp = whatsApp;
        PhoneNumberId = datos.PhoneNumberId;
        Prefijo = datos.Prefijo;
        ZonaHoraria = datos.ZonaHoraria;
    }

    private static string TextoAcotado(string? texto, int maximo, string campo)
    {
        var recortado = texto?.Trim() ?? string.Empty;
        if (recortado.Length is 0 || recortado.Length > maximo)
        {
            throw new ArgumentException($"El {campo} debe tener entre 1 y {maximo} caracteres.", campo);
        }

        return recortado;
    }

    [GeneratedRegex(@"^[a-z0-9-]{3,40}\z")]
    private static partial Regex PatronSlugRegex();

    [GeneratedRegex(@"^[0-9]{1,30}\z")]
    private static partial Regex PatronPhoneNumberIdRegex();

    [GeneratedRegex(@"^[A-Z]{1,3}\z")]
    private static partial Regex PatronPrefijoRegex();
}
