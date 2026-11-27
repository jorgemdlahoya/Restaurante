using Restaurante.Domain.Menu;

namespace Restaurante.Domain.Tests.Menu;

public class TituloWhatsAppTests
{
    private static string Rep(string s, int n) => string.Concat(Enumerable.Repeat(s, n));

    public static TheoryData<string, string?, string, bool> Casos => new()
    {
        { "Pozoles tradicionales de la casa", "Pozoles", "Pozoles", false },
        { Rep("a", 24), null, Rep("a", 24), false },
        { Rep("a", 25), null, Rep("a", 23) + "…", true },
        { "Pozoles tradicionales de la casa", null, "Pozoles tradicionales d…", true },
        { Rep("🌮", 12), null, Rep("🌮", 12), false },
        { Rep("🌮", 13), null, Rep("🌮", 11) + "…", true },
        { Rep("a", 21) + "🌮" + "bbb", null, Rep("a", 21) + "🌮" + "…", true },
    };

    [Theory(DisplayName = "005/RF-11: calcula el título en WhatsApp")]
    [MemberData(nameof(Casos))]
    public void Calcular_AplicaLaRegla(string nombre, string? corto, string titulo, bool seRecorta)
    {
        Assert.Equal(titulo, TituloWhatsApp.Calcular(nombre, corto));
        Assert.Equal(seRecorta, TituloWhatsApp.SeRecorta(nombre, corto));
    }
}
