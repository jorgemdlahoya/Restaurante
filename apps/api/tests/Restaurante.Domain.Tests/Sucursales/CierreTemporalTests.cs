using Restaurante.Domain.Sucursales;

namespace Restaurante.Domain.Tests.Sucursales;

public class CierreTemporalTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private static readonly Guid SucursalId = Guid.NewGuid();
    private static readonly DateTimeOffset Ahora = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "003/RF-10: Crear guarda los datos y recorta el motivo")]
    public void Crear_Valido_GuardaDatos()
    {
        var c = CierreTemporal.Crear(RestauranteId, SucursalId, Ahora.AddDays(1), Ahora.AddDays(2), "  Navidad ", Ahora);

        Assert.NotEqual(Guid.Empty, c.Id);
        Assert.Equal(RestauranteId, c.RestauranteId);
        Assert.Equal(SucursalId, c.SucursalId);
        Assert.Equal(Ahora.AddDays(1), c.Inicio);
        Assert.Equal(Ahora.AddDays(2), c.Fin);
        Assert.Equal("Navidad", c.Motivo);
        Assert.Equal(Ahora, c.CreadoEn);
        Assert.Null(c.CanceladoEn);
    }

    [Theory(DisplayName = "003/RF-10: motivo vacío o en blanco queda nulo")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_MotivoVacio_QuedaNulo(string? motivo)
    {
        var c = CierreTemporal.Crear(RestauranteId, SucursalId, Ahora.AddDays(1), Ahora.AddDays(2), motivo, Ahora);

        Assert.Null(c.Motivo);
    }

    [Fact(DisplayName = "003/RF-10: motivo de 120 caracteres tras recortar es válido")]
    public void Crear_MotivoDeCientoVeinte_EsValido()
    {
        var c = CierreTemporal.Crear(RestauranteId, SucursalId, Ahora.AddDays(1), Ahora.AddDays(2), " " + new string('a', 120) + " ", Ahora);

        Assert.Equal(120, c.Motivo!.Length);
    }

    [Theory(DisplayName = "003/RF-10: Crear rechaza fin no posterior al inicio, fin pasado y motivo largo")]
    [InlineData(2, 2, null)]
    [InlineData(3, 2, null)]
    [InlineData(-3, -1, null)]
    [InlineData(-1, 0, null)]
    [InlineData(1, 2, "LONGITUD_121")]
    public void Crear_Invalido_Lanza(int inicioDias, int finDias, string? motivo)
    {
        if (motivo == "LONGITUD_121")
        {
            motivo = new string('a', 121);
        }

        Assert.Throws<ArgumentException>(() =>
            CierreTemporal.Crear(RestauranteId, SucursalId, Ahora.AddDays(inicioDias), Ahora.AddDays(finDias), motivo, Ahora));
    }

    [Fact(DisplayName = "003/RF-11: cancelar un cierre vigente adelanta su fin y deja de aplicar")]
    public void Cancelar_Vigente_AdelantaElFin()
    {
        var c = CierreTemporal.Crear(RestauranteId, SucursalId, Ahora.AddHours(1), Ahora.AddDays(2), null, Ahora);
        var cuando = Ahora.AddHours(5);
        Assert.True(c.AplicaEn(cuando));

        c.Cancelar(cuando);

        Assert.False(c.AplicaEn(cuando));
        Assert.Equal(cuando, c.Fin);
        Assert.Equal(Ahora.AddHours(1), c.Inicio);
        Assert.Equal(cuando, c.CanceladoEn);
    }

    [Fact(DisplayName = "003/RF-11: cancelar un cierre futuro lo deja sin duración")]
    public void Cancelar_Futuro_LoDejaSinDuracion()
    {
        var c = CierreTemporal.Crear(RestauranteId, SucursalId, Ahora.AddDays(1), Ahora.AddDays(2), null, Ahora);

        c.Cancelar(Ahora);

        Assert.Equal(Ahora, c.Inicio);
        Assert.Equal(Ahora, c.Fin);
        Assert.Equal(Ahora, c.CanceladoEn);
        Assert.False(c.AplicaEn(Ahora));
    }

    [Fact(DisplayName = "003/RF-10: un cierre aplica desde su inicio y no en su fin")]
    public void AplicaEn_InicioInclusivoFinExclusivo()
    {
        var inicio = Ahora.AddDays(1);
        var fin = Ahora.AddDays(2);
        var c = CierreTemporal.Crear(RestauranteId, SucursalId, inicio, fin, null, Ahora);

        Assert.False(c.AplicaEn(inicio.AddTicks(-1)));
        Assert.True(c.AplicaEn(inicio));
        Assert.True(c.AplicaEn(fin.AddTicks(-1)));
        Assert.False(c.AplicaEn(fin));
    }
}
