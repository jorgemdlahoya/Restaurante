using Restaurante.Domain.Menu;

namespace Restaurante.Domain.Tests.Menu;

public class PlatilloTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private static readonly Guid CategoriaId = Guid.NewGuid();

    [Fact(DisplayName = "006/RF-02: un platillo nuevo está activo y con datos recortados")]
    public void Crear_PlatilloNuevo_EstaActivoYRecortaDatos()
    {
        var p = Platillo.Crear(
            RestauranteId, CategoriaId,
            new DatosPlatillo("  Pozole rojo ", "  ", "  ", 2, [" Crema ", "Cebolla"]), Ahora);

        Assert.NotEqual(Guid.Empty, p.Id);
        Assert.Equal(RestauranteId, p.RestauranteId);
        Assert.Equal(CategoriaId, p.CategoriaId);
        Assert.Equal("Pozole rojo", p.Nombre);
        Assert.Equal("pozole rojo", p.NombreNormalizado);
        Assert.Null(p.NombreCorto);
        Assert.Null(p.NombreCortoNormalizado);
        Assert.Null(p.Descripcion);
        Assert.Equal(2, p.Orden);
        Assert.Equal(["Crema", "Cebolla"], p.Ingredientes);
        Assert.True(p.Activo);
        Assert.Equal(Ahora, p.CreadoEn);
        Assert.Null(p.EliminadaEn);
    }

    public static TheoryData<string, string?, string?, int> DatosInvalidos => new()
    {
        { "", null, null, 1 },
        { new string('a', 61), null, null, 1 },
        { "Pozole", new string('a', 25), null, 1 },
        { "Pozole", null, new string('a', 301), 1 },
        { "Pozole", null, null, 0 },
        { "Pozole", null, null, -1 },
    };

    [Theory(DisplayName = "006/RF-02: Crear y Editar rechazan formatos inválidos")]
    [MemberData(nameof(DatosInvalidos))]
    public void CrearYEditar_FormatoInvalido_LanzaArgumentException(
        string nombre, string? corto, string? descripcion, int orden)
    {
        var datos = new DatosPlatillo(nombre, corto, descripcion, orden, []);
        Assert.Throws<ArgumentException>(() => Platillo.Crear(RestauranteId, CategoriaId, datos, Ahora));

        var p = Platillo.Crear(RestauranteId, CategoriaId, new DatosPlatillo("Pozole", null, null, 1, []), Ahora);
        Assert.Throws<ArgumentException>(() => p.Editar(Guid.NewGuid(), datos));
    }

    [Fact(DisplayName = "006/RF-02: Crear y Editar aceptan los límites exactos")]
    public void CrearYEditar_LimitesExactos_Aceptan()
    {
        var datos = new DatosPlatillo(new string('a', 60), new string('b', 24), new string('c', 300), 1, []);
        var p = Platillo.Crear(RestauranteId, CategoriaId, datos, Ahora);
        p.Editar(CategoriaId, datos);

        Assert.Equal(60, p.Nombre.Length);
        Assert.Equal(24, p.NombreCorto!.Length);
        Assert.Equal(300, p.Descripcion!.Length);
    }

    [Fact(DisplayName = "006/RF-02: Crear rechaza ingredientes inválidos")]
    public void Crear_IngredientesInvalidos_LanzaArgumentException()
    {
        var datos = new DatosPlatillo("Pozole", null, null, 1, ["a", " A "]);
        Assert.Throws<ArgumentException>(() => Platillo.Crear(RestauranteId, CategoriaId, datos, Ahora));
    }

    [Fact(DisplayName = "006/RF-02: Editar inválido no cambia el platillo")]
    public void Editar_Invalido_NoCambiaElPlatillo()
    {
        var p = Platillo.Crear(
            RestauranteId, CategoriaId, new DatosPlatillo("Pozole", "Poz", "Rico", 2, ["Crema"]), Ahora);

        Assert.Throws<ArgumentException>(() =>
            p.Editar(Guid.NewGuid(), new DatosPlatillo("Menudo", "M", "Otro", 0, ["Chile"])));
        Assert.Throws<ArgumentException>(() =>
            p.Editar(Guid.NewGuid(), new DatosPlatillo("Menudo", "M", "Otro", 5, ["a", "A"])));

        Assert.Equal(CategoriaId, p.CategoriaId);
        Assert.Equal("Pozole", p.Nombre);
        Assert.Equal("Poz", p.NombreCorto);
        Assert.Equal("Rico", p.Descripcion);
        Assert.Equal(2, p.Orden);
        Assert.Equal(["Crema"], p.Ingredientes);
    }

    [Fact(DisplayName = "006/RF-02: Editar cambia la categoría y los ingredientes")]
    public void Editar_CambiaCategoriaEIngredientes()
    {
        var p = Platillo.Crear(
            RestauranteId, CategoriaId, new DatosPlatillo("Pozole", "Poz", "Rico", 2, ["Crema"]), Ahora);
        var otra = Guid.NewGuid();

        p.Editar(otra, new DatosPlatillo(" Menudo ", "  ", " Con maíz ", 3, [" Chile ", "Limón"]));

        Assert.Equal(otra, p.CategoriaId);
        Assert.Equal("Menudo", p.Nombre);
        Assert.Equal("menudo", p.NombreNormalizado);
        Assert.Null(p.NombreCorto);
        Assert.Null(p.NombreCortoNormalizado);
        Assert.Equal("Con maíz", p.Descripcion);
        Assert.Equal(3, p.Orden);
        Assert.Equal(["Chile", "Limón"], p.Ingredientes);
    }

    [Fact(DisplayName = "006/RF-02: Activar y Desactivar cambian el estado")]
    public void ActivarYDesactivar_CambianEstado()
    {
        var p = Platillo.Crear(RestauranteId, CategoriaId, new DatosPlatillo("Pozole", null, null, 1, []), Ahora);
        p.Desactivar();
        Assert.False(p.Activo);
        p.Activar();
        Assert.True(p.Activo);
    }

    [Fact(DisplayName = "006/RF-09: eliminar marca la fecha y conserva los datos")]
    public void Eliminar_MarcaFechaYConservaDatos()
    {
        var p = Platillo.Crear(
            RestauranteId, CategoriaId, new DatosPlatillo("Pozole", "Poz", "Rico", 1, ["Crema"]), Ahora);
        var despues = Ahora.AddDays(1);

        p.Eliminar(despues);

        Assert.Equal(despues, p.EliminadaEn);
        Assert.Equal("Pozole", p.Nombre);
        Assert.Equal("Poz", p.NombreCorto);
        Assert.Equal(["Crema"], p.Ingredientes);
    }
}
