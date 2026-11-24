using Restaurante.Domain.Restaurantes;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Domain.Tests.Restaurantes;

public class RestauranteTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Paleta PaletaBase = new("#F97316", "#FF6600", null, null, null);

    [Fact(DisplayName = "002/RF-01: un restaurante nuevo está activo con nombre recortado, slug y paleta")]
    public void Crear_RestauranteNuevo_EstaActivoYRecortaNombre()
    {
        var r = RestauranteEntidad.Crear("  La Esquina ", "la-esquina", PaletaBase, Ahora);

        Assert.Equal("La Esquina", r.Nombre);
        Assert.Equal("la-esquina", r.Slug);
        Assert.True(r.Activo);
        Assert.Equal(PaletaBase, r.Paleta);
        Assert.Equal(Ahora, r.CreadoEn);
        Assert.NotEqual(Guid.Empty, r.Id);
    }

    [Theory(DisplayName = "002/RF-01: Crear rechaza slug o nombre inválidos")]
    [InlineData("La Esquina", "AB")]
    [InlineData("La Esquina", "con espacio")]
    [InlineData("La Esquina", "ab")]
    [InlineData("La Esquina", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("La Esquina", "abc\n")]
    [InlineData("", "la-esquina")]
    [InlineData("   ", "la-esquina")]
    public void Crear_DatosInvalidos_Lanza(string nombre, string slug)
    {
        Assert.Throws<ArgumentException>(() => RestauranteEntidad.Crear(nombre, slug, PaletaBase, Ahora));
    }

    [Fact(DisplayName = "002/RF-01: Crear rechaza un nombre de 81 caracteres y acepta uno de 80")]
    public void Crear_NombreLargo_Lanza()
    {
        Assert.Throws<ArgumentException>(() =>
            RestauranteEntidad.Crear(new string('a', 81), "la-esquina", PaletaBase, Ahora));
        var r = RestauranteEntidad.Crear(new string('a', 80), "la-esquina", PaletaBase, Ahora);
        Assert.Equal(80, r.Nombre.Length);
    }

    [Fact(DisplayName = "002/RF-04: cambiar nombre y paleta conserva el slug")]
    public void Cambiar_NombreYPaleta_ConservaSlug()
    {
        var r = RestauranteEntidad.Crear("La Esquina", "la-esquina", PaletaBase, Ahora);
        var nueva = new Paleta("#000000", null, null, null, null);

        r.CambiarNombre("  Otro Nombre ");
        r.CambiarPaleta(nueva);

        Assert.Equal("Otro Nombre", r.Nombre);
        Assert.Equal(nueva, r.Paleta);
        Assert.Equal("la-esquina", r.Slug);
        Assert.Throws<ArgumentException>(() => r.CambiarNombre(" "));
    }

    [Fact(DisplayName = "002/RF-06: desactivar y reactivar cambia sólo el estado")]
    public void DesactivarYActivar_CambiaSoloEstado()
    {
        var r = RestauranteEntidad.Crear("La Esquina", "la-esquina", PaletaBase, Ahora);

        r.Desactivar();
        Assert.False(r.Activo);

        r.Activar();
        Assert.True(r.Activo);
        Assert.Equal("La Esquina", r.Nombre);
        Assert.Equal("la-esquina", r.Slug);
        Assert.Equal(PaletaBase, r.Paleta);
    }

    [Fact(DisplayName = "002/RF-05: un logo conserva contenido, tipo y etag y se reemplaza")]
    public void Logo_CrearYReemplazar()
    {
        var id = Guid.NewGuid();
        var logo = LogoRestaurante.Crear(id, [1, 2, 3], "image/png", "\"abc\"", Ahora);
        Assert.Equal(id, logo.RestauranteId);
        Assert.Equal("image/png", logo.Tipo);

        var despues = Ahora.AddHours(1);
        logo.Reemplazar([9, 9], "image/webp", "\"def\"", despues);

        Assert.Equal(new byte[] { 9, 9 }, logo.Contenido);
        Assert.Equal("image/webp", logo.Tipo);
        Assert.Equal("\"def\"", logo.ETag);
        Assert.Equal(despues, logo.ActualizadoEn);
        Assert.Equal(id, logo.RestauranteId);
    }
}
