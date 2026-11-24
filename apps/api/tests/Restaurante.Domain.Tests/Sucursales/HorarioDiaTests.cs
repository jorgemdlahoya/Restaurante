using Restaurante.Domain.Sucursales;

namespace Restaurante.Domain.Tests.Sucursales;

public class HorarioDiaTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private static readonly Guid SucursalId = Guid.NewGuid();

    [Fact(DisplayName = "003/RF-08: un día con apertura y cierre está abierto; sin horas está cerrado")]
    public void Crear_ConHorasEstaAbierto_SinHorasEstaCerrado()
    {
        var abierto = HorarioDia.Crear(RestauranteId, SucursalId, DiaSemana.Martes, new TimeOnly(9, 0), new TimeOnly(18, 0));
        var cerrado = HorarioDia.Crear(RestauranteId, SucursalId, DiaSemana.Domingo, null, null);

        Assert.False(abierto.Cerrado);
        Assert.Equal(new TimeOnly(9, 0), abierto.Apertura);
        Assert.Equal(new TimeOnly(18, 0), abierto.Cierre);
        Assert.Equal(RestauranteId, abierto.RestauranteId);
        Assert.Equal(SucursalId, abierto.SucursalId);
        Assert.Equal(DiaSemana.Martes, abierto.Dia);
        Assert.True(cerrado.Cerrado);
        Assert.Null(cerrado.Apertura);
        Assert.Null(cerrado.Cierre);
    }

    [Fact(DisplayName = "003/RF-09: apertura igual al cierre o una sola hora lanza ArgumentException")]
    public void Crear_HorasInvalidas_Lanza()
    {
        var nueve = new TimeOnly(9, 0);
        Assert.Throws<ArgumentException>(() => HorarioDia.Crear(RestauranteId, SucursalId, DiaSemana.Lunes, nueve, nueve));
        Assert.Throws<ArgumentException>(() => HorarioDia.Crear(RestauranteId, SucursalId, DiaSemana.Lunes, nueve, null));
        Assert.Throws<ArgumentException>(() => HorarioDia.Crear(RestauranteId, SucursalId, DiaSemana.Lunes, null, nueve));

        var h = HorarioDia.Crear(RestauranteId, SucursalId, DiaSemana.Lunes, nueve, new TimeOnly(17, 0));
        Assert.Throws<ArgumentException>(() => h.Cambiar(nueve, nueve));
        Assert.Throws<ArgumentException>(() => h.Cambiar(nueve, null));
        Assert.Equal(new TimeOnly(17, 0), h.Cierre);
    }

    [Fact(DisplayName = "003/RF-09: un cierre anterior a la apertura cruza la medianoche")]
    public void CruzaMedianoche_CierreAnteriorALaApertura_EsVerdadero()
    {
        var nocturno = HorarioDia.Crear(RestauranteId, SucursalId, DiaSemana.Domingo, new TimeOnly(18, 0), new TimeOnly(2, 0));
        var diurno = HorarioDia.Crear(RestauranteId, SucursalId, DiaSemana.Lunes, new TimeOnly(9, 0), new TimeOnly(18, 0));
        var cerrado = HorarioDia.Crear(RestauranteId, SucursalId, DiaSemana.Martes, null, null);

        Assert.True(nocturno.CruzaMedianoche);
        Assert.False(diurno.CruzaMedianoche);
        Assert.False(cerrado.CruzaMedianoche);
    }

    [Fact(DisplayName = "003/RF-08: la semana nueva tiene los siete días cerrados")]
    public void SemanaCerrada_TieneSieteDiasCerrados()
    {
        var semana = HorarioDia.SemanaCerrada(RestauranteId, SucursalId);

        Assert.Equal(7, semana.Count);
        Assert.Equal(
            new[] { DiaSemana.Lunes, DiaSemana.Martes, DiaSemana.Miercoles, DiaSemana.Jueves, DiaSemana.Viernes, DiaSemana.Sabado, DiaSemana.Domingo },
            semana.Select(h => h.Dia));
        Assert.All(semana, h =>
        {
            Assert.True(h.Cerrado);
            Assert.Equal(RestauranteId, h.RestauranteId);
            Assert.Equal(SucursalId, h.SucursalId);
        });
    }

    [Theory(DisplayName = "003/RF-08: los días se convierten a texto y desde texto")]
    [InlineData(DiaSemana.Lunes, "lunes")]
    [InlineData(DiaSemana.Martes, "martes")]
    [InlineData(DiaSemana.Miercoles, "miércoles")]
    [InlineData(DiaSemana.Jueves, "jueves")]
    [InlineData(DiaSemana.Viernes, "viernes")]
    [InlineData(DiaSemana.Sabado, "sábado")]
    [InlineData(DiaSemana.Domingo, "domingo")]
    public void Dias_SeConvierten_ATextoYDesdeTexto(DiaSemana dia, string texto)
    {
        Assert.Equal(texto, dia.ComoTexto());
        Assert.True(DiasSemana.TryDesdeTexto(texto, out var leido));
        Assert.Equal(dia, leido);
    }

    [Theory(DisplayName = "003/RF-08: textos de día no válidos se rechazan")]
    [InlineData("Lunes")]
    [InlineData("miercoles")]
    [InlineData("")]
    [InlineData(null)]
    public void TryDesdeTexto_TextoInvalido_DevuelveFalso(string? texto)
    {
        Assert.False(DiasSemana.TryDesdeTexto(texto, out _));
    }

    [Theory(DisplayName = "003/RF-08: DayOfWeek se mapea con lunes = 0")]
    [InlineData(DayOfWeek.Monday, DiaSemana.Lunes)]
    [InlineData(DayOfWeek.Wednesday, DiaSemana.Miercoles)]
    [InlineData(DayOfWeek.Saturday, DiaSemana.Sabado)]
    [InlineData(DayOfWeek.Sunday, DiaSemana.Domingo)]
    public void Desde_DayOfWeek_MapeaConLunesCero(DayOfWeek origen, DiaSemana esperado)
    {
        Assert.Equal(esperado, DiasSemana.Desde(origen));
    }
}
