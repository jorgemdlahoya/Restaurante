using Restaurante.Domain.Comun;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Domain.Tests.Sucursales;

public class SucursalTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid RestauranteId = Guid.NewGuid();

    private static DatosSucursal Datos(
        string nombre = "Centro",
        string direccion = "Av. Juárez 1",
        string telefono = "+525512345678",
        string whatsApp = "+525512345679",
        string? phoneNumberId = null,
        string prefijo = "A",
        string zona = "America/Mexico_City") =>
        new(nombre, direccion, telefono, whatsApp, phoneNumberId, prefijo, zona);

    [Fact(DisplayName = "003/RF-01: una sucursal nueva está activa y con datos recortados y normalizados")]
    public void Crear_SucursalNueva_EstaActivaYNormalizaDatos()
    {
        var datos = new DatosSucursal(
            "  Centro ", "Av. Juárez 1", "+52 55 1234 5678", "+52-55-1234-5679", null, "A", "America/Mexico_City");

        var s = Sucursal.Crear(RestauranteId, "centro", datos, Ahora);

        Assert.NotEqual(Guid.Empty, s.Id);
        Assert.Equal(RestauranteId, s.RestauranteId);
        Assert.Equal("Centro", s.Nombre);
        Assert.Equal("centro", s.Slug);
        Assert.Equal("+525512345678", s.Telefono);
        Assert.Equal("+525512345679", s.WhatsApp);
        Assert.Equal("A", s.Prefijo);
        Assert.Equal("America/Mexico_City", s.ZonaHoraria);
        Assert.Equal(Ahora, s.CreadaEn);
        Assert.True(s.Activa);
        Assert.IsAssignableFrom<IEntidadDeRestaurante>(s);
    }

    [Fact(DisplayName = "003/RF-01: Crear rechaza el WhatsApp sin +52")]
    public void Crear_WhatsAppSinPrefijoPais_Rechaza()
    {
        Assert.Throws<ArgumentException>(() =>
            Sucursal.Crear(RestauranteId, "centro", Datos(whatsApp: "55-1234-5679"), Ahora));
    }

    [Theory(DisplayName = "003/RF-01: Crear rechaza formatos inválidos")]
    [InlineData("slug", "Centro")]
    [InlineData("slug", "ab")]
    [InlineData("slug", "abc\n")]
    [InlineData("telefono", "5512345678")]
    [InlineData("prefijo", "a")]
    [InlineData("prefijo", "ABCD")]
    [InlineData("prefijo", "A\n")]
    [InlineData("zona", "Europe/Madrid")]
    [InlineData("phone", "12a")]
    [InlineData("nombre81", "")]
    [InlineData("direccionVacia", "")]
    public void Crear_FormatoInvalido_LanzaArgumentException(string campo, string valor)
    {
        var slug = "centro";
        var datos = Datos();
        switch (campo)
        {
            case "slug": slug = valor; break;
            case "telefono": datos = Datos(telefono: valor); break;
            case "prefijo": datos = Datos(prefijo: valor); break;
            case "zona": datos = Datos(zona: valor); break;
            case "phone": datos = Datos(phoneNumberId: valor); break;
            case "nombre81": datos = Datos(nombre: new string('a', 81)); break;
            case "direccionVacia": datos = Datos(direccion: ""); break;
        }

        Assert.Throws<ArgumentException>(() => Sucursal.Crear(RestauranteId, slug, datos, Ahora));
    }

    [Fact(DisplayName = "003/RF-01: Crear acepta phone_number_id numérico y lo conserva")]
    public void Crear_PhoneNumberIdNumerico_Acepta()
    {
        var s = Sucursal.Crear(RestauranteId, "centro", Datos(phoneNumberId: "1234567890"), Ahora);

        Assert.Equal("1234567890", s.PhoneNumberId);
    }

    [Fact(DisplayName = "003/RF-01: ZonasHorarias admite las 12 zonas y rechaza otras")]
    public void ZonasHorarias_Admite12()
    {
        Assert.Equal(12, ZonasHorarias.Admitidas.Count);
        Assert.Equal("America/Mexico_City", ZonasHorarias.PorDefecto);
        Assert.True(ZonasHorarias.EsAdmitida("America/Tijuana"));
        Assert.False(ZonasHorarias.EsAdmitida("Europe/Madrid"));
        Assert.False(ZonasHorarias.EsAdmitida(null));
    }

    [Fact(DisplayName = "003/RF-05: Editar cambia todo menos el slug")]
    public void Editar_CambiaTodoMenosSlug()
    {
        var s = Sucursal.Crear(RestauranteId, "centro", Datos(), Ahora);

        s.Editar(new DatosSucursal(
            " Norte ", "Calle 2", "+52 81 1111 2222", "+528133334444", "999", "BC", "America/Tijuana"));

        Assert.Equal("centro", s.Slug);
        Assert.Equal("Norte", s.Nombre);
        Assert.Equal("Calle 2", s.Direccion);
        Assert.Equal("+528111112222", s.Telefono);
        Assert.Equal("+528133334444", s.WhatsApp);
        Assert.Equal("999", s.PhoneNumberId);
        Assert.Equal("BC", s.Prefijo);
        Assert.Equal("America/Tijuana", s.ZonaHoraria);
    }

    [Fact(DisplayName = "003/RF-05: Editar rechaza formatos inválidos y no altera la sucursal")]
    public void Editar_Invalido_Rechaza()
    {
        var s = Sucursal.Crear(RestauranteId, "centro", Datos(), Ahora);

        Assert.Throws<ArgumentException>(() => s.Editar(Datos(prefijo: "abc")));
        Assert.Equal("A", s.Prefijo);
    }

    [Fact(DisplayName = "003/RF-06: desactivar y reactivar sólo cambia el estado")]
    public void DesactivarYActivar_SoloCambiaEstado()
    {
        var s = Sucursal.Crear(RestauranteId, "centro", Datos(), Ahora);

        s.Desactivar();
        Assert.False(s.Activa);
        Assert.Equal("Centro", s.Nombre);
        Assert.Equal("centro", s.Slug);

        s.Activar();
        Assert.True(s.Activa);
        Assert.Equal("Centro", s.Nombre);
    }
}
