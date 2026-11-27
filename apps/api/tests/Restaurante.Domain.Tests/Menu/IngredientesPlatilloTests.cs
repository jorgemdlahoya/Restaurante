using Restaurante.Domain.Menu;

namespace Restaurante.Domain.Tests.Menu;

public class IngredientesPlatilloTests
{
    public static TheoryData<string?[]?, string[]> Casos => new()
    {
        { Array.Empty<string?>(), Array.Empty<string>() },
        { null, Array.Empty<string>() },
        { Enumerable.Range(0, 31).Select(i => (string?)$"i{i}").ToArray(), new[] { "No puede haber más de 30 ingredientes" } },
        { new string?[] { "Crema", "  " }, new[] { "El ingrediente 2 está vacío" } },
        { new string?[] { "Crema", null }, new[] { "El ingrediente 2 está vacío" } },
        { new string?[] { "a", new string('b', 41) }, new[] { "El ingrediente 2 no puede superar 40 caracteres" } },
        { new string?[] { " crema ", "Crema" }, new[] { "El ingrediente 2 está repetido" } },
        { Enumerable.Range(0, 30).Select(i => (string?)(i.ToString("D2") + new string('x', 38))).ToArray(), Array.Empty<string>() },
        { new string?[] { "", "a", "A", new string('b', 41) }, new[] { "El ingrediente 1 está vacío", "El ingrediente 3 está repetido", "El ingrediente 4 no puede superar 40 caracteres" } },
    };

    [Theory(DisplayName = "006/RF-02: valida los ingredientes")]
    [MemberData(nameof(Casos))]
    public void Errores_ReportaMensajesEnOrden(string?[]? ingredientes, string[] esperados)
    {
        Assert.Equal(esperados, IngredientesPlatillo.Errores(ingredientes));
    }

    [Fact(DisplayName = "006/RF-02: Normalizar recorta y conserva el orden")]
    public void Normalizar_RecortaYConservaOrden()
    {
        Assert.Equal(new[] { "Queso", "Crema" }, IngredientesPlatillo.Normalizar(new string?[] { " Queso ", "Crema" }));
        Assert.Empty(IngredientesPlatillo.Normalizar(null));
    }

    [Fact(DisplayName = "006/RF-02: Normalizar lanza si hay errores")]
    public void Normalizar_ConErrores_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => IngredientesPlatillo.Normalizar(new string?[] { "a", "A" }));
    }
}
