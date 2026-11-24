using Restaurante.Application.Features.Sucursales.EstadoAtencion;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Tests.Features.Sucursales;

public class CalculadoraEstadoAtencionTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private static readonly Guid SucursalId = Guid.NewGuid();
    private static readonly TimeZoneInfo Cdmx = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
    private static readonly TimeZoneInfo Tijuana = TimeZoneInfo.FindSystemTimeZoneById("America/Tijuana");
    private static readonly TimeSpan DesfaseCdmx = TimeSpan.FromHours(-6);

    // 2027-01-04 es lunes; America/Mexico_City está en UTC-6 todo el año.
    private static DateTimeOffset LocalCdmx(int dia, int hora, int minuto = 0) =>
        new(2027, 1, dia, hora, minuto, 0, DesfaseCdmx);

    private static IReadOnlyList<HorarioDia> Semana(params (DiaSemana Dia, string Apertura, string Cierre)[] abiertos) =>
        Enum.GetValues<DiaSemana>()
            .Select(d =>
            {
                var abierto = abiertos.Where(a => a.Dia == d).ToList();
                return abierto.Count == 0
                    ? HorarioDia.Crear(RestauranteId, SucursalId, d, null, null)
                    : HorarioDia.Crear(
                        RestauranteId, SucursalId, d, TimeOnly.Parse(abierto[0].Apertura), TimeOnly.Parse(abierto[0].Cierre));
            })
            .ToList();

    private static CierreTemporal Cierre(DateTimeOffset inicio, DateTimeOffset fin) =>
        CierreTemporal.Crear(RestauranteId, SucursalId, inicio, fin, null, inicio.AddDays(-30));

    [Fact(DisplayName = "003/RF-12: dentro del horario está abierta")]
    public void Dentro_del_horario_esta_abierta()
    {
        var estado = CalculadoraEstadoAtencion.Calcular(
            LocalCdmx(4, 12), Cdmx, Semana((DiaSemana.Lunes, "09:00", "18:00")), []);

        Assert.True(estado.Abierta);
        Assert.Null(estado.Mensaje);
    }

    [Fact(DisplayName = "003/RF-13: antes de abrir dice hoy")]
    public void Antes_de_abrir_dice_hoy()
    {
        var estado = CalculadoraEstadoAtencion.Calcular(
            LocalCdmx(4, 7), Cdmx, Semana((DiaSemana.Lunes, "09:00", "18:00")), []);

        Assert.False(estado.Abierta);
        Assert.Equal("Estamos cerrados, abrimos hoy a las 09:00", estado.Mensaje);
    }

    [Fact(DisplayName = "003/RF-13: después de cerrar dice mañana")]
    public void Despues_de_cerrar_dice_manana()
    {
        var estado = CalculadoraEstadoAtencion.Calcular(
            LocalCdmx(4, 19), Cdmx,
            Semana((DiaSemana.Lunes, "09:00", "18:00"), (DiaSemana.Martes, "09:00", "18:00")), []);

        Assert.False(estado.Abierta);
        Assert.Equal("Estamos cerrados, abrimos mañana a las 09:00", estado.Mensaje);
    }

    [Fact(DisplayName = "003/RF-13: con días cerrados dice el nombre del día")]
    public void Con_dias_cerrados_dice_el_nombre_del_dia()
    {
        var estado = CalculadoraEstadoAtencion.Calcular(
            LocalCdmx(4, 12), Cdmx, Semana((DiaSemana.Viernes, "10:00", "22:00")), []);

        Assert.False(estado.Abierta);
        Assert.Equal("Estamos cerrados, abrimos viernes a las 10:00", estado.Mensaje);
    }

    [Fact(DisplayName = "003/RF-12: un turno que cruza la medianoche sigue abierto de madrugada")]
    public void Turno_que_cruza_medianoche_sigue_abierto_de_madrugada()
    {
        var estado = CalculadoraEstadoAtencion.Calcular(
            LocalCdmx(4, 1), Cdmx, Semana((DiaSemana.Domingo, "18:00", "02:00")), []);

        Assert.True(estado.Abierta);
        Assert.Null(estado.Mensaje);
    }

    [Fact(DisplayName = "003/RF-12: un cierre temporal vigente cierra la sucursal y la próxima apertura lo salta")]
    public void Cierre_vigente_cierra_y_la_proxima_apertura_lo_salta()
    {
        var horario = Semana(
            (DiaSemana.Lunes, "09:00", "18:00"),
            (DiaSemana.Martes, "09:00", "18:00"),
            (DiaSemana.Miercoles, "09:00", "18:00"));
        var cierre = Cierre(LocalCdmx(4, 0), LocalCdmx(5, 23, 59));

        var estado = CalculadoraEstadoAtencion.Calcular(LocalCdmx(4, 12), Cdmx, horario, [cierre]);

        Assert.False(estado.Abierta);
        Assert.Equal("Estamos cerrados, abrimos miércoles a las 09:00", estado.Mensaje);
    }

    [Fact(DisplayName = "003/RF-12: un cierre futuro no cierra antes de su inicio")]
    public void Cierre_futuro_no_cierra_antes_de_su_inicio()
    {
        var horario = Semana((DiaSemana.Lunes, "09:00", "18:00"), (DiaSemana.Martes, "09:00", "18:00"));
        var cierre = Cierre(LocalCdmx(5, 0), LocalCdmx(6, 0));

        var estado = CalculadoraEstadoAtencion.Calcular(LocalCdmx(4, 12), Cdmx, horario, [cierre]);

        Assert.True(estado.Abierta);
        Assert.Null(estado.Mensaje);
    }

    [Fact(DisplayName = "003/RF-13: sin aperturas en 14 días dice por el momento")]
    public void Sin_aperturas_dice_por_el_momento()
    {
        var estado = CalculadoraEstadoAtencion.Calcular(
            LocalCdmx(4, 12), Cdmx, HorarioDia.SemanaCerrada(RestauranteId, SucursalId), []);

        Assert.False(estado.Abierta);
        Assert.Equal("Estamos cerrados por el momento", estado.Mensaje);
    }

    [Fact(DisplayName = "003/RF-12: usa la zona de la sucursal")]
    public void Usa_la_zona_de_la_sucursal()
    {
        // 17:30 UTC del lunes 2027-01-04 son las 09:30 en Tijuana (UTC-8 en invierno) y las 11:30 en CDMX.
        var ahora = new DateTimeOffset(2027, 1, 4, 17, 30, 0, TimeSpan.Zero);
        var horario = Semana((DiaSemana.Lunes, "09:00", "18:00"));

        var enTijuana = CalculadoraEstadoAtencion.Calcular(ahora, Tijuana, horario, []);
        var temprano = CalculadoraEstadoAtencion.Calcular(ahora.AddHours(-1), Tijuana, horario, []);

        Assert.True(enTijuana.Abierta);
        Assert.Equal("Estamos cerrados, abrimos hoy a las 09:00", temprano.Mensaje);
    }
}
