using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Sucursales.EstadoAtencion;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Tests.Features.Sucursales;

public class ObtenerEstadoAtencionTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();

    // 2026-11-24 (martes) 18:00Z = 12:00 en Ciudad de México (UTC-6).
    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 11, 24, 18, 0, 0, TimeSpan.Zero));
    private readonly SucursalRepositoryEnMemoria _sucursales = new(new ContextoRestauranteFalso(RestauranteId));
    private readonly CierreTemporalRepositoryEnMemoria _cierres;
    private readonly Sucursal _sucursal;

    public ObtenerEstadoAtencionTests()
    {
        _sucursal = Sucursal.Crear(
            RestauranteId,
            "centro",
            new DatosSucursal("Sucursal centro", "Calle 1", "+525500000000", "+525511111111", null, "C", ZonasHorarias.PorDefecto),
            _reloj.Ahora);
        _cierres = new CierreTemporalRepositoryEnMemoria(new ContextoSucursalFalso(_sucursal.Id));
    }

    private void SembrarHorario(params (DiaSemana Dia, string Apertura, string Cierre)[] abiertos)
    {
        var horario = Enum.GetValues<DiaSemana>()
            .Select(d =>
            {
                var abierto = abiertos.Where(a => a.Dia == d).ToList();
                return abierto.Count == 0
                    ? HorarioDia.Crear(RestauranteId, _sucursal.Id, d, null, null)
                    : HorarioDia.Crear(
                        RestauranteId, _sucursal.Id, d, TimeOnly.Parse(abierto[0].Apertura), TimeOnly.Parse(abierto[0].Cierre));
            })
            .ToList();
        _sucursales.Agregar(_sucursal, horario);
    }

    private Task<EstadoAtencionDto> ConsultarAsync(Guid sucursalId) =>
        new ObtenerEstadoAtencionHandler(_sucursales, _cierres, new ContextoSucursalFalso(sucursalId), _reloj)
            .Handle(new ObtenerEstadoAtencionQuery(), CancellationToken.None);

    [Fact(DisplayName = "003/RF-12: el estado de una sucursal abierta responde abierta")]
    public async Task Abierta_dentro_del_horario()
    {
        SembrarHorario((DiaSemana.Martes, "09:00", "18:00"));

        var estado = await ConsultarAsync(_sucursal.Id);

        Assert.True(estado.Abierta);
        Assert.Null(estado.Mensaje);
    }

    [Fact(DisplayName = "003/RF-13: cerrada devuelve el mensaje con la próxima apertura")]
    public async Task Cerrada_devuelve_proxima_apertura()
    {
        SembrarHorario((DiaSemana.Miercoles, "09:00", "18:00"));

        var estado = await ConsultarAsync(_sucursal.Id);

        Assert.False(estado.Abierta);
        Assert.Equal("Estamos cerrados, abrimos mañana a las 09:00", estado.Mensaje);
    }

    [Fact(DisplayName = "003/RF-14: una sucursal inactiva da 404 Sucursal no disponible")]
    public async Task Inactiva_no_disponible()
    {
        SembrarHorario((DiaSemana.Martes, "09:00", "18:00"));
        _sucursal.Desactivar();

        await Assert.ThrowsAsync<SucursalNoDisponibleException>(() => ConsultarAsync(_sucursal.Id));
    }

    [Fact(DisplayName = "003/RF-14: una sucursal inexistente da 404 Sucursal no disponible")]
    public async Task Inexistente_no_disponible()
    {
        SembrarHorario((DiaSemana.Martes, "09:00", "18:00"));

        await Assert.ThrowsAsync<SucursalNoDisponibleException>(() => ConsultarAsync(Guid.NewGuid()));
    }
}
