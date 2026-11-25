using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Categorias;
using Restaurante.Application.Features.Categorias.CrearCategoria;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Tests.Features.Categorias;

public class CrearCategoriaTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly CategoriaRepositoryEnMemoria _repositorio;
    private readonly RelojFijo _reloj = new();

    public CrearCategoriaTests() => _repositorio = new(_contexto);

    private CrearCategoriaHandler CrearHandler() =>
        new(_repositorio, new ComprobadorUnicidadCategoria(_repositorio), _reloj, _contexto);

    [Fact(DisplayName = "005/RF-01: crea la categoría activa en el restaurante del contexto")]
    public async Task CreaCategoriaActivaEnElRestauranteDelContexto()
    {
        var dto = await CrearHandler().Handle(
            new CrearCategoriaCommand(new DatosCategoriaDto("Pozoles", "Pozole", 1)), CancellationToken.None);

        var guardada = Assert.Single(_repositorio.Categorias);
        Assert.Equal(RestauranteId, guardada.RestauranteId);
        Assert.True(guardada.Activa);
        Assert.Equal(_reloj.Ahora, guardada.CreadaEn);
        Assert.Equal(1, _repositorio.GuardadosLlamados);
        Assert.Equal(CategoriaDto.Desde(guardada), dto);
        Assert.Equal("Pozoles", dto.Nombre);
        Assert.Equal("Pozole", dto.NombreCorto);
        Assert.Equal(1, dto.Orden);
    }

    [Fact(DisplayName = "005/RF-02: un nombre corto en blanco se guarda como ausente")]
    public async Task NombreCortoEnBlancoSeGuardaComoAusente()
    {
        var dto = await CrearHandler().Handle(
            new CrearCategoriaCommand(new DatosCategoriaDto(" Postres ", "   ", 2)), CancellationToken.None);

        Assert.Null(dto.NombreCorto);
        Assert.Equal("Postres", dto.Nombre);
    }

    [Fact(DisplayName = "005/RF-03: nombre y nombre corto repetidos lanzan un solo conflicto con ambos campos")]
    public async Task RepetidosLanzanUnConflictoConAmbosCampos()
    {
        _repositorio.Agregar(Categoria.Crear(RestauranteId, new DatosCategoria("Bebidas", "Beb", 1), _reloj.Ahora));

        var ex = await Assert.ThrowsAsync<ConflictoCamposException>(() => CrearHandler().Handle(
            new CrearCategoriaCommand(new DatosCategoriaDto(" BEBIDAS ", "beb", 2)), CancellationToken.None));

        Assert.Equal([MensajesCategoria.NombreRepetido], ex.Errores["nombre"]);
        Assert.Equal([MensajesCategoria.NombreCortoRepetido], ex.Errores["nombreCorto"]);
        Assert.Single(_repositorio.Categorias);
        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "005/RF-03: sólo el nombre repetido deja fuera nombreCorto")]
    public async Task SoloNombreRepetido()
    {
        _repositorio.Agregar(Categoria.Crear(RestauranteId, new DatosCategoria("Bebidas", null, 1), _reloj.Ahora));

        var ex = await Assert.ThrowsAsync<ConflictoCamposException>(() => CrearHandler().Handle(
            new CrearCategoriaCommand(new DatosCategoriaDto("bebidas", "Otra", 2)), CancellationToken.None));

        Assert.Equal(["nombre"], ex.Errores.Keys);
    }

    [Fact(DisplayName = "005/RF-03: el mismo nombre en otro restaurante se acepta")]
    public async Task MismoNombreEnOtroRestauranteSeAcepta()
    {
        _repositorio.Agregar(Categoria.Crear(Guid.NewGuid(), new DatosCategoria("Bebidas", null, 1), _reloj.Ahora));

        await CrearHandler().Handle(
            new CrearCategoriaCommand(new DatosCategoriaDto("Bebidas", null, 1)), CancellationToken.None);

        Assert.Equal(2, _repositorio.Categorias.Count);
    }
}
