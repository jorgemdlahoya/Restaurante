using FluentValidation;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Sucursales.Cierres;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Tests.Features.Sucursales;

public class CierresTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();

    // 2026-11-24 18:00Z = 12:00 en Ciudad de México (UTC-6) y 10:00 en Tijuana (UTC-8).
    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 11, 24, 18, 0, 0, TimeSpan.Zero));
    private readonly SucursalRepositoryEnMemoria _sucursales = new(new ContextoRestauranteFalso(RestauranteId));
    private readonly Sucursal _sucursal;
    private readonly Sucursal _otra;
    private ContextoSucursalFalso _contexto;
    private readonly CierreTemporalRepositoryEnMemoria _cierres;

    public CierresTests()
    {
        _sucursal = Sembrar("centro", "+525511111111", "C", ZonasHorarias.PorDefecto);
        _otra = Sembrar("norte", "+525522222222", "N", ZonasHorarias.PorDefecto);
        _contexto = new ContextoSucursalFalso(_sucursal.Id);
        _cierres = new CierreTemporalRepositoryEnMemoria(new DelegadoContexto(() => _contexto.SucursalId));
    }

    private sealed class DelegadoContexto(Func<Guid?> sucursalId) : Restaurante.Application.Abstracciones.IContextoSucursal
    {
        public Guid? SucursalId => sucursalId();
    }

    private Sucursal Sembrar(string slug, string whatsApp, string prefijo, string zona)
    {
        var sucursal = Sucursal.Crear(
            RestauranteId,
            slug,
            new DatosSucursal("Sucursal " + slug, "Calle 1", "+525500000000", whatsApp, null, prefijo, zona),
            _reloj.Ahora);
        _sucursales.Agregar(sucursal, HorarioDia.SemanaCerrada(RestauranteId, sucursal.Id));
        return sucursal;
    }

    private CrearCierreHandler CrearHandler() => new(_sucursales, _cierres, _contexto, _reloj);

    private ListarCierresHandler ListarHandler() => new(_sucursales, _cierres, _contexto, _reloj);

    private CancelarCierreHandler CancelarHandler() => new(_sucursales, _cierres, _contexto, _reloj);

    private void EnSucursal(Sucursal sucursal) => _contexto = new ContextoSucursalFalso(sucursal.Id);

    private Task<IReadOnlyList<CierreTemporalDto>> ListarAsync() =>
        ListarHandler().Handle(new ListarCierresQuery(), CancellationToken.None);

    [Fact(DisplayName = "003/RF-10: crear un cierre sin inicio empieza ahora y se lista")]
    public async Task CrearSinInicioEmpiezaAhora()
    {
        var dto = await CrearHandler().Handle(
            new CrearCierreCommand(null, "2026-11-25T10:00", " Remodelación "), CancellationToken.None);

        Assert.Equal("2026-11-24T12:00", dto.Inicio);
        Assert.Equal("2026-11-25T10:00", dto.Fin);
        Assert.Equal("Remodelación", dto.Motivo);
        var guardado = Assert.Single(_cierres.Cierres);
        Assert.Equal(_reloj.Ahora, guardado.Inicio);
        Assert.Equal(RestauranteId, guardado.RestauranteId);
        Assert.Equal(_sucursal.Id, guardado.SucursalId);
        Assert.Equal(dto, Assert.Single(await ListarAsync()));
    }

    [Theory(DisplayName = "003/RF-10: fin anterior al inicio, fin pasado o fecha mal escrita dan 400")]
    [InlineData("2026-12-01T10:00", "2026-12-01T09:00", "fin", "El fin debe ser posterior al inicio")]
    [InlineData("2026-12-01T10:00", "2026-12-01T10:00", "fin", "El fin debe ser posterior al inicio")]
    [InlineData("2026-11-01T10:00", "2026-11-10T10:00", "fin", "El fin no puede estar en el pasado")]
    [InlineData(null, "2026-11-24T12:00", "fin", "El fin debe ser posterior al inicio")]
    [InlineData("mañana", "2026-12-01T10:00", "Inicio", "La fecha debe tener el formato yyyy-MM-ddTHH:mm")]
    [InlineData("2026-12-01T10:00", "2026-12-01 10:00", "Fin", "La fecha debe tener el formato yyyy-MM-ddTHH:mm")]
    [InlineData("2026-12-01T10:00", "2026-13-01T10:00", "Fin", "La fecha debe tener el formato yyyy-MM-ddTHH:mm")]
    [InlineData("2026-12-01T10:00", null, "Fin", "La fecha debe tener el formato yyyy-MM-ddTHH:mm")]
    public async Task FechasInvalidas(string? inicio, string? fin, string campo, string mensaje)
    {
        var comando = new CrearCierreCommand(inicio, fin!, null);
        var resultado = await new CrearCierreValidator().ValidateAsync(comando);

        List<FluentValidation.Results.ValidationFailure> errores;
        if (!resultado.IsValid)
        {
            errores = resultado.Errors;
        }
        else
        {
            errores = (await Assert.ThrowsAsync<ValidationException>(
                () => CrearHandler().Handle(comando, CancellationToken.None))).Errors.ToList();
        }

        Assert.Contains(errores, e => e.PropertyName == campo && e.ErrorMessage == mensaje);
        Assert.Empty(_cierres.Cierres);
    }

    [Fact(DisplayName = "003/RF-10: un motivo de más de 120 caracteres da 400")]
    public async Task MotivoDemasiadoLargo()
    {
        var resultado = await new CrearCierreValidator().ValidateAsync(
            new CrearCierreCommand(null, "2026-12-01T10:00", new string('m', 121)));

        Assert.Contains(resultado.Errors, e => e.PropertyName == "Motivo" && e.ErrorMessage == "El motivo no puede superar 120 caracteres");
        Assert.True((await new CrearCierreValidator().ValidateAsync(
            new CrearCierreCommand(null, "2026-12-01T10:00", new string('m', 120)))).IsValid);
    }

    [Fact(DisplayName = "003/RF-10: las fechas se interpretan en la zona de la sucursal")]
    public async Task FechasEnZonaDeLaSucursal()
    {
        var tijuana = Sembrar("tijuana", "+525533333333", "T", "America/Tijuana");
        EnSucursal(tijuana);

        var dto = await CrearHandler().Handle(
            new CrearCierreCommand("2026-12-24T09:00", "2026-12-25T09:00", "Navidad"), CancellationToken.None);

        var guardado = Assert.Single(_cierres.Cierres);
        Assert.Equal(new DateTimeOffset(2026, 12, 24, 17, 0, 0, TimeSpan.Zero), guardado.Inicio);
        Assert.Equal(new DateTimeOffset(2026, 12, 25, 17, 0, 0, TimeSpan.Zero), guardado.Fin);
        Assert.Equal("2026-12-24T09:00", dto.Inicio);
        Assert.Equal("2026-12-25T09:00", dto.Fin);
        Assert.Equal("2026-12-24T09:00", Assert.Single(await ListarAsync()).Inicio);
    }

    [Fact(DisplayName = "003/RF-11: cancelar un cierre lo saca de la lista de vigentes")]
    public async Task CancelarLoSacaDeLaLista()
    {
        var creado = await CrearHandler().Handle(
            new CrearCierreCommand("2026-12-01T10:00", "2026-12-02T10:00", null), CancellationToken.None);

        var cancelado = await CancelarHandler().Handle(new CancelarCierreCommand(creado.Id), CancellationToken.None);

        Assert.Equal(creado.Id, cancelado.Id);
        Assert.Empty(await ListarAsync());
        var guardado = Assert.Single(_cierres.Cierres);
        Assert.Equal(_reloj.Ahora, guardado.CanceladoEn);
    }

    [Fact(DisplayName = "003/RF-11: cancelar un cierre ya cancelado o ya terminado da 404 y no lo reactiva")]
    public async Task CancelarCerradoOTerminadoDa404()
    {
        var yaCancelado = CierreTemporal.Crear(
            RestauranteId, _sucursal.Id, _reloj.Ahora, _reloj.Ahora.AddDays(2), null, _reloj.Ahora);
        yaCancelado.Cancelar(_reloj.Ahora);
        var terminado = CierreTemporal.Crear(
            RestauranteId, _sucursal.Id, _reloj.Ahora.AddDays(-3), _reloj.Ahora.AddMinutes(-5), null, _reloj.Ahora.AddDays(-4));
        _cierres.Agregar(yaCancelado);
        _cierres.Agregar(terminado);
        var finTerminado = terminado.Fin;
        var finCancelado = yaCancelado.Fin;

        foreach (var id in new[] { yaCancelado.Id, terminado.Id })
        {
            var error = await Assert.ThrowsAsync<NoEncontradoException>(
                () => CancelarHandler().Handle(new CancelarCierreCommand(id), CancellationToken.None));
            Assert.Equal("Cierre no encontrado", error.Message);
        }

        Assert.Equal(finCancelado, yaCancelado.Fin);
        Assert.Equal(finTerminado, terminado.Fin);
        Assert.Null(terminado.CanceladoEn);
    }

    [Fact(DisplayName = "003/RF-15: cancelar desde la sucursal A un cierre de la B da 404")]
    public async Task CancelarDeOtraSucursalDa404()
    {
        EnSucursal(_otra);
        var deB = await CrearHandler().Handle(
            new CrearCierreCommand("2026-12-01T10:00", "2026-12-02T10:00", null), CancellationToken.None);
        EnSucursal(_sucursal);

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => CancelarHandler().Handle(new CancelarCierreCommand(deB.Id), CancellationToken.None));
        var inexistente = await Assert.ThrowsAsync<NoEncontradoException>(
            () => CancelarHandler().Handle(new CancelarCierreCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("Cierre no encontrado", error.Message);
        Assert.Equal("Cierre no encontrado", inexistente.Message);
        Assert.Empty(await ListarAsync());
        EnSucursal(_otra);
        Assert.Equal(deB, Assert.Single(await ListarAsync()));
        Assert.Null(Assert.Single(_cierres.Cierres).CanceladoEn);
    }
}
