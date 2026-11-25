using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Categorias;
using Restaurante.Application.Features.Categorias.CambiarEstadoCategoria;
using Restaurante.Application.Features.Categorias.EditarCategoria;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Tests.Features.Categorias;

public class EditarCategoriaTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly CategoriaRepositoryEnMemoria _repositorio;
    private readonly RelojFijo _reloj = new();

    public EditarCategoriaTests() => _repositorio = new(_contexto);

    private EditarCategoriaHandler Editar() => new(_repositorio, new ComprobadorUnicidadCategoria(_repositorio));

    private Categoria Sembrar(string nombre, string? corto = null, int orden = 1, Guid? restauranteId = null)
    {
        var categoria = Categoria.Crear(restauranteId ?? RestauranteId, new DatosCategoria(nombre, corto, orden), _reloj.Ahora);
        _repositorio.Agregar(categoria);
        return categoria;
    }

    [Fact(DisplayName = "005/RF-05: edita nombre, nombre corto y orden y guarda")]
    public async Task EditaYGuarda()
    {
        var c = Sembrar("Bebidas", "Beb", 1);

        var dto = await Editar().Handle(
            new EditarCategoriaCommand(c.Id, new DatosCategoriaDto(" Refrescos ", "Ref", 3)), CancellationToken.None);

        Assert.Equal("Refrescos", dto.Nombre);
        Assert.Equal("Ref", dto.NombreCorto);
        Assert.Equal(3, dto.Orden);
        Assert.Equal(CategoriaDto.Desde(c), dto);
        Assert.Equal(1, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "005/RF-05: renombrar cambiando sólo mayúsculas no choca consigo misma")]
    public async Task RenombrarSoloMayusculasNoChocaConsigoMisma()
    {
        var c = Sembrar("bebidas", "beb");

        var dto = await Editar().Handle(
            new EditarCategoriaCommand(c.Id, new DatosCategoriaDto("Bebidas", "Beb", 1)), CancellationToken.None);

        Assert.Equal("Bebidas", dto.Nombre);
        Assert.Equal("Beb", dto.NombreCorto);
    }

    [Fact(DisplayName = "005/RF-05: el nombre y el nombre corto de otra categoría lanzan conflicto con ambos campos")]
    public async Task ConflictoConOtraCategoria()
    {
        Sembrar("Bebidas", "Beb");
        var c = Sembrar("Postres", "Pos", 2);

        var ex = await Assert.ThrowsAsync<ConflictoCamposException>(() => Editar().Handle(
            new EditarCategoriaCommand(c.Id, new DatosCategoriaDto("BEBIDAS", " beb ", 2)), CancellationToken.None));

        Assert.Equal([MensajesCategoria.NombreRepetido], ex.Errores["nombre"]);
        Assert.Equal([MensajesCategoria.NombreCortoRepetido], ex.Errores["nombreCorto"]);
        Assert.Equal("Postres", c.Nombre);
        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "005/RF-05: un nombre corto en blanco lo quita")]
    public async Task NombreCortoEnBlancoLoQuita()
    {
        var c = Sembrar("Bebidas", "Beb");

        var dto = await Editar().Handle(
            new EditarCategoriaCommand(c.Id, new DatosCategoriaDto("Bebidas", "   ", 1)), CancellationToken.None);

        Assert.Null(dto.NombreCorto);
        Assert.Null(c.NombreCorto);
    }

    [Fact(DisplayName = "005/RF-05: una categoría inexistente lanza no encontrado")]
    public async Task InexistenteLanzaNoEncontrado()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() => Editar().Handle(
            new EditarCategoriaCommand(Guid.NewGuid(), new DatosCategoriaDto("Bebidas", null, 1)), CancellationToken.None));

        Assert.Equal(MensajesCategoria.NoEncontrada, ex.Message);
    }

    [Fact(DisplayName = "005/RF-06: desactivar y activar conservan la categoría y guardan")]
    public async Task CambiaEstado()
    {
        var c = Sembrar("Bebidas");
        var handler = new CambiarEstadoCategoriaHandler(_repositorio);

        var apagada = await handler.Handle(new CambiarEstadoCategoriaCommand(c.Id, false), CancellationToken.None);
        Assert.False(apagada.Activa);
        Assert.False(c.Activa);

        var encendida = await handler.Handle(new CambiarEstadoCategoriaCommand(c.Id, true), CancellationToken.None);
        Assert.True(encendida.Activa);
        Assert.Equal(2, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "005/RF-08: cambiar el estado de una categoría de otro restaurante lanza no encontrado")]
    public async Task EstadoDeOtroRestauranteLanzaNoEncontrado()
    {
        var ajena = Sembrar("Ajena", restauranteId: Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() =>
            new CambiarEstadoCategoriaHandler(_repositorio)
                .Handle(new CambiarEstadoCategoriaCommand(ajena.Id, false), CancellationToken.None));

        Assert.Equal(MensajesCategoria.NoEncontrada, ex.Message);
        Assert.True(ajena.Activa);
    }
}
