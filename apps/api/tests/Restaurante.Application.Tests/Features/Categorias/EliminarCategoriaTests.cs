using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Categorias;
using Restaurante.Application.Features.Categorias.EliminarCategoria;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Tests.Features.Categorias;

public class EliminarCategoriaTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly CategoriaRepositoryEnMemoria _repositorio;
    private readonly PlatilloRepositoryEnMemoria _platillos;
    private readonly RelojFijo _reloj = new();

    public EliminarCategoriaTests()
    {
        _repositorio = new(_contexto);
        _platillos = new(_contexto);
    }

    private EliminarCategoriaHandler Eliminar() => new(_repositorio, _platillos, _reloj);

    private void SembrarPlatillo(Categoria c, string nombre, bool activo = true, bool eliminado = false)
    {
        var p = Platillo.Crear(RestauranteId, c.Id, new DatosPlatillo(nombre, null, null, 1, []), _reloj.Ahora);
        if (!activo) p.Desactivar();
        if (eliminado) p.Eliminar(_reloj.Ahora);
        _platillos.Agregar(p);
    }

    [Theory(DisplayName = "006/RF-12: una categoría con platillos lanza conflicto con la cantidad y no se elimina")]
    [InlineData(1, "Esta categoría tiene 1 platillo")]
    [InlineData(2, "Esta categoría tiene 2 platillos")]
    public async Task ConPlatillosLanzaConflicto(int cantidad, string mensaje)
    {
        var c = Sembrar("Pozoles");
        for (var i = 0; i < cantidad; i++) SembrarPlatillo(c, $"P{i}", activo: i == 0);

        var ex = await Assert.ThrowsAsync<CategoriaConPlatillosException>(() =>
            Eliminar().Handle(new EliminarCategoriaCommand(c.Id), CancellationToken.None));

        Assert.Equal(cantidad, ex.Cantidad);
        Assert.Equal(mensaje, ex.Message);
        Assert.Null(c.EliminadaEn);
        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "006/RF-12: los platillos eliminados no impiden eliminar la categoría")]
    public async Task PlatillosEliminadosNoCuentan()
    {
        var c = Sembrar("Pozoles");
        SembrarPlatillo(c, "Viejo", eliminado: true);

        await Eliminar().Handle(new EliminarCategoriaCommand(c.Id), CancellationToken.None);

        Assert.NotNull(c.EliminadaEn);
    }

    private Categoria Sembrar(string nombre, Guid? restauranteId = null)
    {
        var categoria = Categoria.Crear(restauranteId ?? RestauranteId, new DatosCategoria(nombre, null, 1), _reloj.Ahora);
        _repositorio.Agregar(categoria);
        return categoria;
    }

    [Fact(DisplayName = "005/RF-07: marca la categoría como eliminada con la hora del reloj y guarda sin borrarla")]
    public async Task MarcaEliminadaYGuarda()
    {
        var c = Sembrar("Bebidas");
        var hora = new DateTimeOffset(2031, 5, 6, 7, 8, 9, TimeSpan.Zero);
        _reloj.Ahora = hora;

        await Eliminar().Handle(new EliminarCategoriaCommand(c.Id), CancellationToken.None);

        Assert.Equal(hora, c.EliminadaEn);
        Assert.Contains(c, _repositorio.Categorias);
        Assert.Equal(1, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "005/RF-08: una categoría de otro restaurante lanza no encontrado y no guarda")]
    public async Task OtroRestauranteNoEncontrado()
    {
        var c = Sembrar("Ajena", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Eliminar().Handle(new EliminarCategoriaCommand(c.Id), CancellationToken.None));

        Assert.Equal(MensajesCategoria.NoEncontrada, ex.Message);
        Assert.Null(c.EliminadaEn);
        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "005/RF-08: una categoría ya eliminada lanza no encontrado")]
    public async Task YaEliminadaNoEncontrada()
    {
        var c = Sembrar("Bebidas");
        await Eliminar().Handle(new EliminarCategoriaCommand(c.Id), CancellationToken.None);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Eliminar().Handle(new EliminarCategoriaCommand(c.Id), CancellationToken.None));
    }
}
