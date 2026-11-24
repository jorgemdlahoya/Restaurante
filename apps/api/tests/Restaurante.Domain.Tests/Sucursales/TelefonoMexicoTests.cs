using Restaurante.Domain.Comun;

namespace Restaurante.Domain.Tests.Sucursales;

public class TelefonoMexicoTests
{
    [Theory(DisplayName = "003/RF-01: normaliza teléfonos mexicanos")]
    [InlineData("+52 55 1234 5678", "+525512345678")]
    [InlineData("+52-55-1234-5678", "+525512345678")]
    [InlineData("5512345678", null)]
    [InlineData("+15512345678", null)]
    [InlineData("+52551234567", null)]
    [InlineData("+525512345678\n", null)]
    [InlineData(null, null)]
    public void Normalizar_DevuelveE164ONulo(string? entrada, string? esperado)
    {
        Assert.Equal(esperado, TelefonoMexico.Normalizar(entrada));
    }
}
