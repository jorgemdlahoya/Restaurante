using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Sucursales;
using Restaurante.Application.Features.Sucursales.CrearSucursal;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Tests.Features.Sucursales;

public class CrearSucursalTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly SucursalRepositoryEnMemoria _repositorio;
    private readonly RelojFijo _reloj = new();

    public CrearSucursalTests() => _repositorio = new(_contexto);

    private CrearSucursalHandler CrearHandler() => new(_repositorio, _reloj, _contexto);

    private static DatosSucursalDto Datos(
        string nombre = "Centro",
        string telefono = "+52 55 1234 5678",
        string whatsApp = "+525512345679",
        string? phoneNumberId = null,
        string prefijo = "C",
        string? zona = null) =>
        new(nombre, "Av. Juárez 1", telefono, whatsApp, phoneNumberId, prefijo, zona);

    private static DatosSucursal Existente(string nombre, string whatsApp, string prefijo, string? phoneNumberId = null) =>
        new(nombre, "Calle 1", "+525500000000", whatsApp, phoneNumberId, prefijo, ZonasHorarias.PorDefecto);

    [Fact(DisplayName = "003/RF-01: crea la sucursal activa con la semana cerrada en el restaurante del contexto")]
    public async Task CreaSucursalActivaConSemanaCerrada()
    {
        var dto = await CrearHandler().Handle(new CrearSucursalCommand("centro", Datos()), CancellationToken.None);

        var guardada = Assert.Single(_repositorio.Sucursales);
        Assert.Equal(RestauranteId, guardada.RestauranteId);
        Assert.True(guardada.Activa);
        Assert.Equal(_reloj.Ahora, guardada.CreadaEn);
        Assert.Equal("+525512345678", guardada.Telefono);
        Assert.Equal(ZonasHorarias.PorDefecto, guardada.ZonaHoraria);
        Assert.Equal(7, _repositorio.Horarios.Count);
        Assert.All(_repositorio.Horarios, h =>
        {
            Assert.True(h.Cerrado);
            Assert.Equal(guardada.Id, h.SucursalId);
            Assert.Equal(RestauranteId, h.RestauranteId);
        });
        Assert.Equal(1, _repositorio.GuardadosLlamados);
        Assert.Equal(SucursalDto.Desde(guardada), dto);
        Assert.Equal("centro", dto.Slug);
    }

    [Fact(DisplayName = "003/RF-03: nombre, slug o prefijo repetidos en el restaurante lanzan ConflictoException")]
    public async Task RepetidosEnElRestauranteLanzanConflicto()
    {
        _repositorio.Agregar(
            Sucursal.Crear(RestauranteId, "centro", Existente("Centro", "+525500000001", "C"), _reloj.Ahora), []);

        var porNombre = await Assert.ThrowsAsync<ConflictoException>(() => CrearHandler().Handle(
            new CrearSucursalCommand("otra", Datos(nombre: "Centro", whatsApp: "+525500000002", prefijo: "D")),
            CancellationToken.None));
        var porSlug = await Assert.ThrowsAsync<ConflictoException>(() => CrearHandler().Handle(
            new CrearSucursalCommand("centro", Datos(nombre: "Otra", whatsApp: "+525500000002", prefijo: "D")),
            CancellationToken.None));
        var porPrefijo = await Assert.ThrowsAsync<ConflictoException>(() => CrearHandler().Handle(
            new CrearSucursalCommand("otra", Datos(nombre: "Otra", whatsApp: "+525500000002", prefijo: "C")),
            CancellationToken.None));

        Assert.Equal("Ya existe una sucursal con ese nombre", porNombre.Message);
        Assert.Equal("Ya existe una sucursal con ese slug", porSlug.Message);
        Assert.Equal("Ya existe una sucursal con ese prefijo", porPrefijo.Message);
        Assert.Single(_repositorio.Sucursales);
        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "003/RF-03: un WhatsApp o phone_number_id de cualquier restaurante lanza ConflictoException")]
    public async Task WhatsAppYPhoneNumberIdSonUnicosEnLaPlataforma()
    {
        _repositorio.Agregar(
            Sucursal.Crear(Guid.NewGuid(), "ajena", Existente("Ajena", "+525500000001", "A", "12345"), _reloj.Ahora), []);

        var porWhatsApp = await Assert.ThrowsAsync<ConflictoException>(() => CrearHandler().Handle(
            new CrearSucursalCommand("centro", Datos(whatsApp: "+52 55 0000 0001")), CancellationToken.None));
        var porPhoneNumberId = await Assert.ThrowsAsync<ConflictoException>(() => CrearHandler().Handle(
            new CrearSucursalCommand("centro", Datos(phoneNumberId: "12345")), CancellationToken.None));

        Assert.Equal("Ya existe una sucursal con ese WhatsApp", porWhatsApp.Message);
        Assert.Equal("Ya existe una sucursal con ese phone_number_id", porPhoneNumberId.Message);
        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "003/RF-03: el mismo prefijo en otro restaurante se acepta")]
    public async Task MismoPrefijoEnOtroRestauranteSeAcepta()
    {
        _repositorio.Agregar(
            Sucursal.Crear(Guid.NewGuid(), "ajena", Existente("Centro", "+525500000001", "C"), _reloj.Ahora), []);

        var dto = await CrearHandler().Handle(new CrearSucursalCommand("centro", Datos()), CancellationToken.None);

        Assert.Equal("C", dto.Prefijo);
        Assert.Equal(2, _repositorio.Sucursales.Count);
    }
}
