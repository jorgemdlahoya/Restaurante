using Restaurante.Domain.Menu;

namespace Restaurante.Domain.Tests.Menu;

public class CategoriaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid RestauranteId = Guid.NewGuid();

    [Fact(DisplayName = "005/RF-02: una categoría nueva está activa y con datos recortados")]
    public void Crear_CategoriaNueva_EstaActivaYRecortaDatos()
    {
        var c = Categoria.Crear(RestauranteId, new DatosCategoria("  Pozoles ", "   ", 1), Ahora);

        Assert.NotEqual(Guid.Empty, c.Id);
        Assert.Equal(RestauranteId, c.RestauranteId);
        Assert.Equal("Pozoles", c.Nombre);
        Assert.Equal("pozoles", c.NombreNormalizado);
        Assert.Null(c.NombreCorto);
        Assert.Null(c.NombreCortoNormalizado);
        Assert.Equal(1, c.Orden);
        Assert.True(c.Activa);
        Assert.Equal(Ahora, c.CreadaEn);
        Assert.Null(c.EliminadaEn);
    }

    public static TheoryData<string, string?, int> DatosInvalidos => new()
    {
        { "", null, 1 },
        { "   ", null, 1 },
        { new string('a', 61), null, 1 },
        { "Pozoles", new string('a', 25), 1 },
        { "Pozoles", null, 0 },
        { "Pozoles", null, -1 },
    };

    [Theory(DisplayName = "005/RF-02: Crear y Editar rechazan formatos inválidos")]
    [MemberData(nameof(DatosInvalidos))]
    public void CrearYEditar_FormatoInvalido_LanzaArgumentException(string nombre, string? corto, int orden)
    {
        var datos = new DatosCategoria(nombre, corto, orden);
        Assert.Throws<ArgumentException>(() => Categoria.Crear(RestauranteId, datos, Ahora));

        var c = Categoria.Crear(RestauranteId, new DatosCategoria("Pozoles", "Poz", 2), Ahora);
        Assert.Throws<ArgumentException>(() => c.Editar(datos));
        Assert.Equal("Pozoles", c.Nombre);
        Assert.Equal("Poz", c.NombreCorto);
        Assert.Equal(2, c.Orden);
    }

    [Fact(DisplayName = "005/RF-02: Crear y Editar aceptan los límites exactos")]
    public void CrearYEditar_LimitesExactos_Aceptan()
    {
        var datos = new DatosCategoria(new string('a', 60), new string('b', 24), 1);
        var c = Categoria.Crear(RestauranteId, datos, Ahora);
        c.Editar(datos);

        Assert.Equal(60, c.Nombre.Length);
        Assert.Equal(24, c.NombreCorto!.Length);
    }

    [Fact(DisplayName = "005/RF-02: Editar con nombre corto en blanco lo quita")]
    public void Editar_NombreCortoEnBlanco_LoQuita()
    {
        var c = Categoria.Crear(RestauranteId, new DatosCategoria("Pozoles", "Poz", 1), Ahora);
        Assert.Equal("poz", c.NombreCortoNormalizado);

        c.Editar(new DatosCategoria(" Café ", "   ", 3));

        Assert.Null(c.NombreCorto);
        Assert.Null(c.NombreCortoNormalizado);
        Assert.Equal("Café", c.Nombre);
        Assert.Equal("café", c.NombreNormalizado);
        Assert.Equal(3, c.Orden);
    }

    [Fact(DisplayName = "005/RF-02: Activar y Desactivar cambian el estado")]
    public void ActivarYDesactivar_CambianEstado()
    {
        var c = Categoria.Crear(RestauranteId, new DatosCategoria("Pozoles", null, 1), Ahora);
        c.Desactivar();
        Assert.False(c.Activa);
        c.Activar();
        Assert.True(c.Activa);
    }

    [Fact(DisplayName = "005/RF-07: eliminar marca la fecha y conserva los datos")]
    public void Eliminar_MarcaFechaYConservaDatos()
    {
        var c = Categoria.Crear(RestauranteId, new DatosCategoria("Pozoles", "Poz", 1), Ahora);
        var despues = Ahora.AddDays(1);

        c.Eliminar(despues);

        Assert.Equal(despues, c.EliminadaEn);
        Assert.Equal("Pozoles", c.Nombre);
        Assert.Equal("Poz", c.NombreCorto);
        Assert.Equal(1, c.Orden);
    }

    [Fact(DisplayName = "005/RF-02: Normalizar recorta y pasa a minúsculas conservando acentos")]
    public void Normalizar_RecortaYMinusculas()
    {
        Assert.Equal("café", Categoria.Normalizar("  Café "));
        Assert.NotEqual(Categoria.Normalizar("Café"), Categoria.Normalizar("Cafe"));
    }
}
