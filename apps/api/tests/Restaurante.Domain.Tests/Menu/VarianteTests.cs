using Restaurante.Domain.Menu;

namespace Restaurante.Domain.Tests.Menu;

public class VarianteTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private static readonly Guid PlatilloId = Guid.NewGuid();

    [Fact(DisplayName = "007/RF-03: una variante nueva está activa y con datos recortados")]
    public void Crear_VarianteNueva_EstaActivaYRecortaDatos()
    {
        var v = Variante.Crear(RestauranteId, PlatilloId, new DatosVariante("  1 litro ", 45.5m, "  "), Ahora);

        Assert.NotEqual(Guid.Empty, v.Id);
        Assert.Equal(RestauranteId, v.RestauranteId);
        Assert.Equal(PlatilloId, v.PlatilloId);
        Assert.Equal("1 litro", v.Nombre);
        Assert.Equal("1 litro", v.NombreNormalizado);
        Assert.Equal(45.50m, v.Precio);
        Assert.Null(v.Sku);
        Assert.Null(v.SkuNormalizado);
        Assert.True(v.Activa);
        Assert.Equal(Ahora, v.CreadaEn);
        Assert.Null(v.EliminadaEn);
    }

    [Fact(DisplayName = "007/RF-03: el SKU se recorta y se normaliza en mayúsculas")]
    public void Crear_ConSku_NormalizaEnMayusculas()
    {
        var v = Variante.Crear(RestauranteId, PlatilloId, new DatosVariante("Chica", 10m, " tac-01_b "), Ahora);

        Assert.Equal("tac-01_b", v.Sku);
        Assert.Equal("TAC-01_B", v.SkuNormalizado);
    }

    public static TheoryData<string, decimal, string?> DatosInvalidos => new()
    {
        { "", 10m, null },
        { new string('a', 25), 10m, null },
        { "Chica", -0.01m, null },
        { "Chica", 100_000m, null },
        { "Chica", 1.005m, null },
        { "Chica", 10m, "ab c" },
        { "Chica", 10m, new string('a', 41) },
        { "Chica", 10m, "á1" },
    };

    [Theory(DisplayName = "007/RF-03: Crear y Editar rechazan formatos inválidos")]
    [MemberData(nameof(DatosInvalidos))]
    public void CrearYEditar_FormatoInvalido_LanzaArgumentException(string nombre, decimal precio, string? sku)
    {
        var datos = new DatosVariante(nombre, precio, sku);
        Assert.Throws<ArgumentException>(() => Variante.Crear(RestauranteId, PlatilloId, datos, Ahora));

        var v = Variante.Crear(RestauranteId, PlatilloId, new DatosVariante("Chica", 10m, null), Ahora);
        Assert.Throws<ArgumentException>(() => v.Editar(datos));
    }

    [Fact(DisplayName = "007/RF-03: Crear y Editar aceptan los límites exactos")]
    public void CrearYEditar_LimitesExactos_Aceptan()
    {
        var datos = new DatosVariante(new string('a', 24), 99_999.99m, "TAC-01_b");
        var v = Variante.Crear(RestauranteId, PlatilloId, datos, Ahora);
        v.Editar(new DatosVariante("x", 0m, new string('z', 40)));
        v.Editar(datos);

        Assert.Equal(new string('a', 24), v.Nombre);
        Assert.Equal(99_999.99m, v.Precio);
        Assert.Equal("TAC-01_b", v.Sku);
    }

    [Fact(DisplayName = "007/RF-03: Editar inválido no cambia la variante")]
    public void Editar_Invalido_NoCambiaLaVariante()
    {
        var v = Variante.Crear(RestauranteId, PlatilloId, new DatosVariante("Chica", 10m, "abc"), Ahora);

        Assert.Throws<ArgumentException>(() => v.Editar(new DatosVariante("Grande", 1.005m, "xyz")));

        Assert.Equal("Chica", v.Nombre);
        Assert.Equal("chica", v.NombreNormalizado);
        Assert.Equal(10m, v.Precio);
        Assert.Equal("abc", v.Sku);
        Assert.Equal("ABC", v.SkuNormalizado);
    }

    [Fact(DisplayName = "007/RF-03: Editar actualiza nombre, precio y SKU")]
    public void Editar_Valido_ActualizaLosDatos()
    {
        var v = Variante.Crear(RestauranteId, PlatilloId, new DatosVariante("Chica", 10m, "abc"), Ahora);

        v.Editar(new DatosVariante(" Grande ", 45.5m, null));

        Assert.Equal("Grande", v.Nombre);
        Assert.Equal("grande", v.NombreNormalizado);
        Assert.Equal(45.50m, v.Precio);
        Assert.Null(v.Sku);
        Assert.Null(v.SkuNormalizado);
    }

    [Fact(DisplayName = "007/RF-01: la variante Regular se llama Regular, está activa y no tiene SKU")]
    public void Regular_SeLlamaRegularActivaYSinSku()
    {
        var v = Variante.Regular(RestauranteId, PlatilloId, 80m, Ahora);

        Assert.Equal("Regular", Variante.NombreRegular);
        Assert.Equal("Regular", v.Nombre);
        Assert.Equal("regular", v.NombreNormalizado);
        Assert.Equal(80m, v.Precio);
        Assert.Null(v.Sku);
        Assert.True(v.Activa);
        Assert.Equal(PlatilloId, v.PlatilloId);
    }

    [Fact(DisplayName = "007/RF-09: eliminar marca la fecha y conserva los datos")]
    public void Eliminar_MarcaLaFechaYConservaLosDatos()
    {
        var v = Variante.Crear(RestauranteId, PlatilloId, new DatosVariante("Chica", 10m, "abc"), Ahora);
        var despues = Ahora.AddHours(1);

        v.Eliminar(despues);

        Assert.Equal(despues, v.EliminadaEn);
        Assert.Equal("Chica", v.Nombre);
        Assert.Equal(10m, v.Precio);
        Assert.Equal("abc", v.Sku);
    }

    [Fact(DisplayName = "007/RF-03: Activar y Desactivar cambian el estado")]
    public void ActivarDesactivar_CambianElEstado()
    {
        var v = Variante.Crear(RestauranteId, PlatilloId, new DatosVariante("Chica", 10m, null), Ahora);

        v.Desactivar();
        Assert.False(v.Activa);
        v.Activar();
        Assert.True(v.Activa);
    }

    [Fact(DisplayName = "007/RF-03: normaliza nombre y SKU")]
    public void Normalizar_NombreYSku()
    {
        Assert.Equal("1 litro", Variante.NormalizarNombre("  1 LITRO "));
        Assert.Equal("ABC-1", Variante.NormalizarSku("  abc-1 "));
    }
}
