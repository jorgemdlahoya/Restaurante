using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Platillos;
using Restaurante.Application.Features.Platillos.EliminarPlatillo;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Tests.Features.Platillos;

public class EliminarPlatilloTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly PlatilloRepositoryEnMemoria _platillos;
    private readonly RelojFijo _reloj = new();

    public EliminarPlatilloTests() => _platillos = new(_contexto);

    private EliminarPlatilloHandler Handler() => new(_platillos, _reloj);

    private Platillo Sembrar(string nombre, Guid? restauranteId = null)
    {
        var platillo = Platillo.Crear(
            restauranteId ?? RestauranteId, Guid.NewGuid(), new DatosPlatillo(nombre, null, null, 1, []), _reloj.Ahora);
        _platillos.Agregar(platillo);
        return platillo;
    }

    [Fact(DisplayName = "006/RF-09: marca el platillo como eliminado con la hora del reloj y guarda sin borrarlo")]
    public async Task MarcaEliminadoYGuarda()
    {
        var p = Sembrar("Pozole");
        var hora = new DateTimeOffset(2031, 5, 6, 7, 8, 9, TimeSpan.Zero);
        _reloj.Ahora = hora;

        await Handler().Handle(new EliminarPlatilloCommand(p.Id), CancellationToken.None);

        Assert.Equal(hora, p.EliminadaEn);
        Assert.Contains(p, _platillos.Platillos);
        Assert.Equal(1, _platillos.GuardadosLlamados);
    }

    [Fact(DisplayName = "006/RF-10: un platillo de otro restaurante lanza no encontrado y no guarda")]
    public async Task OtroRestauranteNoEncontrado()
    {
        var p = Sembrar("Ajeno", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Handler().Handle(new EliminarPlatilloCommand(p.Id), CancellationToken.None));

        Assert.Equal(MensajesPlatillo.NoEncontrado, ex.Message);
        Assert.Null(p.EliminadaEn);
        Assert.Equal(0, _platillos.GuardadosLlamados);
    }

    [Fact(DisplayName = "006/RF-10: un platillo ya eliminado lanza no encontrado")]
    public async Task YaEliminadoNoEncontrado()
    {
        var p = Sembrar("Pozole");
        await Handler().Handle(new EliminarPlatilloCommand(p.Id), CancellationToken.None);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Handler().Handle(new EliminarPlatilloCommand(p.Id), CancellationToken.None));
    }
}
