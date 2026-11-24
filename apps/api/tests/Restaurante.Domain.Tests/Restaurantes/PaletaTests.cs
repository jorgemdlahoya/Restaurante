using Restaurante.Domain.Restaurantes;

namespace Restaurante.Domain.Tests.Restaurantes;

public class PaletaTests
{
    [Fact(DisplayName = "002/RF-04: Paleta acepta colores #RRGGBB o nulos y rechaza otros formatos")]
    public void Paleta_AceptaHexONulos()
    {
        var paleta = new Paleta("#f97316", null, "#111111", null, "#1F2937");

        Assert.Equal("#f97316", paleta.Principal);
        Assert.Null(paleta.Acento);
        Assert.Equal(Paleta.Vacia, new Paleta(null, null, null, null, null));
    }

    [Theory(DisplayName = "002/RF-04: Paleta rechaza colores con formato inválido")]
    [InlineData("F97316")]
    [InlineData("#FFF")]
    [InlineData("#GGGGGG")]
    [InlineData("#FFFFFF\n")]
    public void Paleta_RechazaFormatoInvalido(string color)
    {
        Assert.Throws<ArgumentException>(() => new Paleta(color, null, null, null, null));
        Assert.Throws<ArgumentException>(() => new Paleta(null, null, null, null, color));
        Assert.False(Paleta.EsColorHex(color));
    }
}
