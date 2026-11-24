using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Sucursales;
using Restaurante.Application.Features.Sucursales.EditarSucursal;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Tests.Features.Sucursales;

public class EditarSucursalTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly SucursalRepositoryEnMemoria _repositorio;
    private readonly RelojFijo _reloj = new();

    public EditarSucursalTests() => _repositorio = new(_contexto);

    private EditarSucursalHandler CrearHandler() => new(_repositorio);

    private Sucursal Sembrar(Guid restauranteId, string slug, string nombre, string whatsApp, string prefijo, string? phoneNumberId = null)
    {
        var sucursal = Sucursal.Crear(
            restauranteId,
            slug,
            new DatosSucursal(nombre, "Calle 1", "+525500000000", whatsApp, phoneNumberId, prefijo, ZonasHorarias.PorDefecto),
            _reloj.Ahora);
        _repositorio.Agregar(sucursal, []);
        return sucursal;
    }

    private static DatosSucursalDto Datos(string nombre, string whatsApp, string prefijo, string? phoneNumberId = null) =>
        new(nombre, "Av. Nueva 2", "+52 55 1234 5678", whatsApp, phoneNumberId, prefijo, "America/Tijuana");

    [Fact(DisplayName = "003/RF-05: editar cambia los datos y conserva el slug")]
    public async Task EditaDatosYConservaSlug()
    {
        var existente = Sembrar(RestauranteId, "centro", "Centro", "+525500000001", "C");

        var dto = await CrearHandler().Handle(
            new EditarSucursalCommand(existente.Id, Datos("Centro Nuevo", "+525500000009", "N")), CancellationToken.None);

        Assert.Equal("centro", dto.Slug);
        Assert.Equal("Centro Nuevo", existente.Nombre);
        Assert.Equal("+525512345678", existente.Telefono);
        Assert.Equal("America/Tijuana", existente.ZonaHoraria);
        Assert.Equal("N", existente.Prefijo);
        Assert.Equal(SucursalDto.Desde(existente), dto);
        Assert.Equal(1, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "003/RF-05: editar con los mismos nombre, prefijo y WhatsApp propios no da conflicto")]
    public async Task ValoresPropiosNoSonConflicto()
    {
        var existente = Sembrar(RestauranteId, "centro", "Centro", "+525500000001", "C", "123");

        var dto = await CrearHandler().Handle(
            new EditarSucursalCommand(existente.Id, Datos("Centro", "+525500000001", "C", "123")), CancellationToken.None);

        Assert.Equal("C", dto.Prefijo);
    }

    [Fact(DisplayName = "003/RF-05: editar con nombre, prefijo, WhatsApp o phone_number_id de otra sucursal lanza ConflictoException")]
    public async Task ValoresDeOtraSucursalLanzanConflicto()
    {
        var existente = Sembrar(RestauranteId, "centro", "Centro", "+525500000001", "C");
        Sembrar(RestauranteId, "norte", "Norte", "+525500000002", "N", "999");
        var handler = CrearHandler();

        await Assert.ThrowsAsync<ConflictoException>(() => handler.Handle(
            new EditarSucursalCommand(existente.Id, Datos("Norte", "+525500000003", "X")), CancellationToken.None));
        await Assert.ThrowsAsync<ConflictoException>(() => handler.Handle(
            new EditarSucursalCommand(existente.Id, Datos("Otra", "+525500000003", "N")), CancellationToken.None));
        await Assert.ThrowsAsync<ConflictoException>(() => handler.Handle(
            new EditarSucursalCommand(existente.Id, Datos("Otra", "+525500000002", "X")), CancellationToken.None));
        await Assert.ThrowsAsync<ConflictoException>(() => handler.Handle(
            new EditarSucursalCommand(existente.Id, Datos("Otra", "+525500000003", "X", "999")), CancellationToken.None));
        Assert.Equal("Centro", existente.Nombre);
        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "003/RF-05: editar una sucursal de otro restaurante o inexistente lanza NoEncontradoException")]
    public async Task AjenaOInexistenteLanzaNoEncontrado()
    {
        var ajena = Sembrar(Guid.NewGuid(), "ajena", "Ajena", "+525500000005", "A");
        var handler = CrearHandler();

        await Assert.ThrowsAsync<NoEncontradoException>(() => handler.Handle(
            new EditarSucursalCommand(ajena.Id, Datos("Nueva", "+525500000009", "N")), CancellationToken.None));
        await Assert.ThrowsAsync<NoEncontradoException>(() => handler.Handle(
            new EditarSucursalCommand(Guid.NewGuid(), Datos("Nueva", "+525500000009", "N")), CancellationToken.None));
        Assert.Equal("Ajena", ajena.Nombre);
    }

    [Fact(DisplayName = "003/RF-04: listar devuelve sólo las sucursales del restaurante, ordenadas por nombre")]
    public async Task ListaSoloLasDelRestaurante()
    {
        Sembrar(RestauranteId, "norte", "Norte", "+525500000002", "N");
        Sembrar(RestauranteId, "centro", "Centro", "+525500000001", "C");
        Sembrar(Guid.NewGuid(), "ajena", "Ajena", "+525500000005", "A");

        var lista = await new Restaurante.Application.Features.Sucursales.ListarSucursales.ListarSucursalesHandler(_repositorio)
            .Handle(new Restaurante.Application.Features.Sucursales.ListarSucursales.ListarSucursalesQuery(), CancellationToken.None);

        Assert.Equal(new[] { "Centro", "Norte" }, lista.Select(s => s.Nombre));
    }

    [Fact(DisplayName = "003/RF-06: cambiar estado desactiva y reactiva; otro restaurante lanza NoEncontradoException")]
    public async Task CambiaEstado()
    {
        var existente = Sembrar(RestauranteId, "centro", "Centro", "+525500000001", "C");
        var ajena = Sembrar(Guid.NewGuid(), "ajena", "Ajena", "+525500000005", "A");
        var handler = new Restaurante.Application.Features.Sucursales.CambiarEstadoSucursal.CambiarEstadoSucursalHandler(_repositorio);

        var apagada = await handler.Handle(
            new Restaurante.Application.Features.Sucursales.CambiarEstadoSucursal.CambiarEstadoSucursalCommand(existente.Id, false),
            CancellationToken.None);
        Assert.False(apagada.Activa);
        Assert.False(existente.Activa);

        var encendida = await handler.Handle(
            new Restaurante.Application.Features.Sucursales.CambiarEstadoSucursal.CambiarEstadoSucursalCommand(existente.Id, true),
            CancellationToken.None);
        Assert.True(encendida.Activa);

        await Assert.ThrowsAsync<NoEncontradoException>(() => handler.Handle(
            new Restaurante.Application.Features.Sucursales.CambiarEstadoSucursal.CambiarEstadoSucursalCommand(ajena.Id, false),
            CancellationToken.None));
        Assert.True(ajena.Activa);
    }
}
