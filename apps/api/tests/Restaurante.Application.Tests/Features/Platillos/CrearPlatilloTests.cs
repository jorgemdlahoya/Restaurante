using FluentValidation;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Categorias;
using Restaurante.Application.Features.Platillos;
using Restaurante.Application.Features.Platillos.CrearPlatillo;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Tests.Features.Platillos;

public class CrearPlatilloTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly CategoriaRepositoryEnMemoria _categorias;
    private readonly PlatilloRepositoryEnMemoria _platillos;
    private readonly RelojFijo _reloj = new();

    public CrearPlatilloTests()
    {
        _categorias = new(_contexto);
        _platillos = new(_contexto);
    }

    private CrearPlatilloHandler CrearHandler() =>
        new(_categorias, _platillos, new ComprobadorUnicidadPlatillo(_platillos), _reloj, _contexto);

    private Categoria SembrarCategoria(string nombre = "Pozoles", Guid? restauranteId = null)
    {
        var categoria = Categoria.Crear(restauranteId ?? RestauranteId, new DatosCategoria(nombre, null, 1), _reloj.Ahora);
        _categorias.Agregar(categoria);
        return categoria;
    }

    private void SembrarPlatillo(Guid categoriaId, string nombre, string? corto = null, Guid? restauranteId = null) =>
        _platillos.Agregar(Platillo.Crear(
            restauranteId ?? RestauranteId, categoriaId, new DatosPlatillo(nombre, corto, null, 1, []), _reloj.Ahora));

    [Fact(DisplayName = "006/RF-01: crea el platillo activo en la categoría")]
    public async Task CreaPlatilloActivoEnLaCategoria()
    {
        var categoria = SembrarCategoria();

        var dto = await CrearHandler().Handle(
            new CrearPlatilloCommand(categoria.Id,
                new DatosPlatilloDto(" Pozole rojo ", "Pozole", "Con maciza", 1, [" Crema ", "Cebolla"])),
            CancellationToken.None);

        var guardado = Assert.Single(_platillos.Platillos);
        Assert.Equal(RestauranteId, guardado.RestauranteId);
        Assert.Equal(categoria.Id, guardado.CategoriaId);
        Assert.True(guardado.Activo);
        Assert.Equal(_reloj.Ahora, guardado.CreadoEn);
        Assert.Equal(1, _platillos.GuardadosLlamados);
        Assert.Equal(PlatilloDto.Desde(guardado, categoria), dto);
        Assert.Equal("Pozole rojo", dto.Nombre);
        Assert.Equal("Pozoles", dto.CategoriaNombre);
        Assert.Equal(["Crema", "Cebolla"], dto.Ingredientes);
        Assert.True(dto.Activo);
    }

    [Fact(DisplayName = "006/RF-01: sin ingredientes se guarda una lista vacía")]
    public async Task SinIngredientesSeGuardaListaVacia()
    {
        var categoria = SembrarCategoria();

        var dto = await CrearHandler().Handle(
            new CrearPlatilloCommand(categoria.Id, new DatosPlatilloDto("Pozole", null, null, 1, null)),
            CancellationToken.None);

        Assert.Empty(dto.Ingredientes);
        Assert.Empty(Assert.Single(_platillos.Platillos).Ingredientes);
    }

    [Fact(DisplayName = "006/RF-03: una categoría de otro restaurante lanza no encontrado")]
    public async Task CategoriaAjenaLanzaNoEncontrado()
    {
        var ajena = SembrarCategoria(restauranteId: Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() => CrearHandler().Handle(
            new CrearPlatilloCommand(ajena.Id, new DatosPlatilloDto("Pozole", null, null, 1, null)),
            CancellationToken.None));

        Assert.Equal(MensajesCategoria.NoEncontrada, ex.Message);
        Assert.Empty(_platillos.Platillos);
    }

    [Fact(DisplayName = "006/RF-03: una categoría inactiva lanza validación en categoriaId")]
    public async Task CategoriaInactivaLanzaValidacion()
    {
        var categoria = SembrarCategoria();
        categoria.Desactivar();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => CrearHandler().Handle(
            new CrearPlatilloCommand(categoria.Id, new DatosPlatilloDto("Pozole", null, null, 1, null)),
            CancellationToken.None));

        var fallo = Assert.Single(ex.Errors);
        Assert.Equal("categoriaId", fallo.PropertyName);
        Assert.Equal("La categoría está inactiva", fallo.ErrorMessage);
        Assert.Empty(_platillos.Platillos);
    }

    [Fact(DisplayName = "006/RF-04: nombre y nombre corto repetidos lanzan un solo conflicto con ambos campos")]
    public async Task RepetidosLanzanUnConflictoConAmbosCampos()
    {
        var categoria = SembrarCategoria();
        SembrarPlatillo(categoria.Id, "Pozole rojo", "Pozole");

        var ex = await Assert.ThrowsAsync<ConflictoCamposException>(() => CrearHandler().Handle(
            new CrearPlatilloCommand(categoria.Id, new DatosPlatilloDto(" POZOLE ROJO ", "pozole", null, 2, null)),
            CancellationToken.None));

        Assert.Equal([MensajesPlatillo.NombreRepetido], ex.Errores["nombre"]);
        Assert.Equal([MensajesPlatillo.NombreCortoRepetido], ex.Errores["nombreCorto"]);
        Assert.Single(_platillos.Platillos);
        Assert.Equal(0, _platillos.GuardadosLlamados);
    }

    [Fact(DisplayName = "006/RF-04: un platillo puede llamarse como una categoría")]
    public async Task PlatilloPuedeLlamarseComoCategoria()
    {
        var categoria = SembrarCategoria("Pozoles");

        var dto = await CrearHandler().Handle(
            new CrearPlatilloCommand(categoria.Id, new DatosPlatilloDto("Pozoles", null, null, 1, null)),
            CancellationToken.None);

        Assert.Equal("Pozoles", dto.Nombre);
    }

    [Fact(DisplayName = "006/RF-04: el mismo nombre en otro restaurante se acepta")]
    public async Task MismoNombreEnOtroRestauranteSeAcepta()
    {
        var categoria = SembrarCategoria();
        SembrarPlatillo(Guid.NewGuid(), "Pozole rojo", "Pozole", restauranteId: Guid.NewGuid());

        await CrearHandler().Handle(
            new CrearPlatilloCommand(categoria.Id, new DatosPlatilloDto("Pozole rojo", "Pozole", null, 1, null)),
            CancellationToken.None);

        Assert.Equal(2, _platillos.Platillos.Count);
    }
}
