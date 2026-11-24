using Restaurante.Domain.Sucursales;

namespace Restaurante.Infrastructure.Tests;

public sealed class ZonasHorariasTests
{
    public static TheoryData<string> Admitidas() => new(ZonasHorarias.Admitidas);

    [Theory(DisplayName = "003/RF-12: las zonas horarias admitidas se resuelven en el servidor")]
    [MemberData(nameof(Admitidas))]
    public void Zona_admitida_se_resuelve(string zona)
    {
        var info = TimeZoneInfo.FindSystemTimeZoneById(zona);

        Assert.Equal(zona, info.Id);
    }
}
