using Restaurante.Domain.Menu;

namespace Restaurante.Domain.Tests.Menu;

public class PrecioVarianteTests
{
    public static TheoryData<decimal?, string[]> Casos => new()
    {
        { null, ["El precio es obligatorio"] },
        { -1m, ["El precio no puede ser negativo"] },
        { 100_000m, ["El precio no puede superar 99,999.99"] },
        { 1.005m, ["El precio debe tener como máximo 2 decimales"] },
        { 0m, [] },
        { 45.5m, [] },
        { 99_999.99m, [] },
    };

    [Theory(DisplayName = "007/RF-03: valida el precio")]
    [MemberData(nameof(Casos))]
    public void Errores_DevuelveLosMensajesEsperados(decimal? precio, string[] esperados)
    {
        Assert.Equal(esperados, PrecioVariante.Errores(precio));
    }

    [Fact(DisplayName = "007/RF-03: el máximo del precio es 99,999.99")]
    public void Maximo_Es99999Con99()
    {
        Assert.Equal(99_999.99m, PrecioVariante.Maximo);
    }
}
