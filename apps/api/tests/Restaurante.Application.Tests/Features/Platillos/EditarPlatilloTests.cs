using FluentValidation;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Categorias;
using Restaurante.Application.Features.Platillos;
using Restaurante.Application.Features.Platillos.CambiarEstadoPlatillo;
using Restaurante.Application.Features.Platillos.EditarPlatillo;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Tests.Features.Platillos;

public class EditarPlatilloTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly CategoriaRepositoryEnMemoria _categorias;
    private readonly PlatilloRepositoryEnMemoria _platillos;
    private readonly RelojFijo _reloj = new();

    public EditarPlatilloTests()
    {
        _categorias = new(_contexto);
        _platillos = new(_contexto);
    }

    private EditarPlatilloHandler Handler() =>
        new(_categorias, _platillos, new ComprobadorUnicidadPlatillo(_platillos));

    private Categoria SembrarCategoria(string nombre = "Pozoles", Guid? restauranteId = null)
    {
        var categoria = Categoria.Crear(restauranteId ?? RestauranteId, new DatosCategoria(nombre, null, 1), _reloj.Ahora);
        _categorias.Agregar(categoria);
        return categoria;
    }

    private Platillo SembrarPlatillo(
        Guid categoriaId, string nombre = "Pozole rojo", string? corto = null, string[]? ingredientes = null,
        Guid? restauranteId = null)
    {
        var platillo = Platillo.Crear(
            restauranteId ?? RestauranteId, categoriaId,
            new DatosPlatillo(nombre, corto, null, 1, ingredientes ?? []), _reloj.Ahora);
        _platillos.Agregar(platillo);
        return platillo;
    }

    private static DatosPlatilloDto Datos(string nombre, string? corto = null, string[]? ingredientes = null) =>
        new(nombre, corto, null, 2, ingredientes);

    private static async Task<ValidationException> ErrorDe(Task tarea) =>
        await Assert.ThrowsAsync<ValidationException>(() => tarea);

    [Fact(DisplayName = "006/RF-07: editar cambia los datos y los ingredientes")]
    public async Task EditarCambiaDatosEIngredientes()
    {
        var categoria = SembrarCategoria();
        var platillo = SembrarPlatillo(categoria.Id, ingredientes: ["Crema"]);

        var dto = await Handler().Handle(
            new EditarPlatilloCommand(platillo.Id, categoria.Id,
                new DatosPlatilloDto(" Pozole verde ", "Verde", "Con pollo", 3, [" Cebolla ", "Limón"])),
            CancellationToken.None);

        Assert.Equal("Pozole verde", platillo.Nombre);
        Assert.Equal("Verde", platillo.NombreCorto);
        Assert.Equal("Con pollo", platillo.Descripcion);
        Assert.Equal(3, platillo.Orden);
        Assert.Equal(["Cebolla", "Limón"], platillo.Ingredientes);
        Assert.Equal(1, _platillos.GuardadosLlamados);
        Assert.Equal(PlatilloDto.Desde(platillo, categoria), dto);
    }

    [Fact(DisplayName = "006/RF-07: renombrar cambiando sólo mayúsculas no choca consigo mismo")]
    public async Task RenombrarSoloMayusculasNoChocaConsigoMismo()
    {
        var categoria = SembrarCategoria();
        var platillo = SembrarPlatillo(categoria.Id, "pozole rojo", "pozole");

        var dto = await Handler().Handle(
            new EditarPlatilloCommand(platillo.Id, categoria.Id, Datos("Pozole Rojo", "Pozole")),
            CancellationToken.None);

        Assert.Equal("Pozole Rojo", dto.Nombre);
        Assert.Equal("Pozole", dto.NombreCorto);
    }

    [Fact(DisplayName = "006/RF-07: mover a otra categoría activa cambia categoriaId y categoriaNombre")]
    public async Task MoverAOtraCategoriaActiva()
    {
        var origen = SembrarCategoria("Pozoles");
        var destino = SembrarCategoria("Postres");
        var platillo = SembrarPlatillo(origen.Id);

        var dto = await Handler().Handle(
            new EditarPlatilloCommand(platillo.Id, destino.Id, Datos("Pozole rojo")), CancellationToken.None);

        Assert.Equal(destino.Id, platillo.CategoriaId);
        Assert.Equal(destino.Id, dto.CategoriaId);
        Assert.Equal("Postres", dto.CategoriaNombre);
    }

    [Fact(DisplayName = "006/RF-07: mover a una categoría inactiva da 400 y quedarse en la actual inactiva da 200")]
    public async Task MoverAInactivaFallaYQuedarseEnLaActualInactivaFunciona()
    {
        var origen = SembrarCategoria("Pozoles");
        var inactiva = SembrarCategoria("Postres");
        inactiva.Desactivar();
        var platillo = SembrarPlatillo(origen.Id);

        var ex = await ErrorDe(Handler().Handle(
            new EditarPlatilloCommand(platillo.Id, inactiva.Id, Datos("Otro")), CancellationToken.None));

        var error = Assert.Single(ex.Errors);
        Assert.Equal("categoriaId", error.PropertyName);
        Assert.Equal("La categoría está inactiva", error.ErrorMessage);
        Assert.Equal("Pozole rojo", platillo.Nombre);
        Assert.Equal(origen.Id, platillo.CategoriaId);

        origen.Desactivar();
        var dto = await Handler().Handle(
            new EditarPlatilloCommand(platillo.Id, origen.Id, Datos("Otro")), CancellationToken.None);
        Assert.Equal("Otro", dto.Nombre);
        Assert.Equal(origen.Id, dto.CategoriaId);
    }

    [Fact(DisplayName = "006/RF-07: una categoría inexistente o de otro restaurante da 400 La categoría no existe")]
    public async Task CategoriaInexistenteOAjenaDa400()
    {
        var categoria = SembrarCategoria();
        var ajena = SembrarCategoria("Ajena", Guid.NewGuid());
        var platillo = SembrarPlatillo(categoria.Id);

        foreach (var id in new[] { Guid.NewGuid(), ajena.Id })
        {
            var ex = await ErrorDe(Handler().Handle(
                new EditarPlatilloCommand(platillo.Id, id, Datos("Otro")), CancellationToken.None));
            var error = Assert.Single(ex.Errors);
            Assert.Equal("categoriaId", error.PropertyName);
            Assert.Equal("La categoría no existe", error.ErrorMessage);
        }

        Assert.Equal("Pozole rojo", platillo.Nombre);
    }

    [Fact(DisplayName = "006/RF-07: sin categoría el validador da La categoría es obligatoria")]
    public void SinCategoriaElValidadorLoRechaza()
    {
        var resultado = new EditarPlatilloValidator().Validate(
            new EditarPlatilloCommand(Guid.NewGuid(), null, Datos("Pozole")));

        var error = Assert.Single(resultado.Errors);
        Assert.Equal("CategoriaId", error.PropertyName);
        Assert.Equal("La categoría es obligatoria", error.ErrorMessage);
    }

    [Fact(DisplayName = "006/RF-07: nombre de otro platillo da 409 en nombre")]
    public async Task NombreDeOtroPlatilloDa409()
    {
        var categoria = SembrarCategoria();
        SembrarPlatillo(categoria.Id, "Pozole verde", "Verde");
        var platillo = SembrarPlatillo(categoria.Id, "Pozole rojo");

        var ex = await Assert.ThrowsAsync<ConflictoCamposException>(() => Handler().Handle(
            new EditarPlatilloCommand(platillo.Id, categoria.Id, Datos(" POZOLE VERDE ", "verde")),
            CancellationToken.None));

        Assert.Contains("nombre", ex.Errores.Keys);
        Assert.Contains("nombreCorto", ex.Errores.Keys);
        Assert.Equal("Pozole rojo", platillo.Nombre);
        Assert.Equal(0, _platillos.GuardadosLlamados);
    }

    [Fact(DisplayName = "006/RF-10: editar un platillo inexistente, eliminado o ajeno lanza no encontrado")]
    public async Task EditarNoDisponibleLanzaNoEncontrado()
    {
        var categoria = SembrarCategoria();
        var eliminado = SembrarPlatillo(categoria.Id, "Borrado");
        eliminado.Eliminar(_reloj.Ahora);
        var ajeno = SembrarPlatillo(categoria.Id, "Ajeno", restauranteId: Guid.NewGuid());

        foreach (var id in new[] { Guid.NewGuid(), eliminado.Id, ajeno.Id })
        {
            var ex = await Assert.ThrowsAsync<NoEncontradoException>(() => Handler().Handle(
                new EditarPlatilloCommand(id, categoria.Id, Datos("Otro")), CancellationToken.None));
            Assert.Equal("Platillo no encontrado", ex.Message);
        }
    }

    [Fact(DisplayName = "006/RF-08: desactivar y activar cambian el estado y lo guardan")]
    public async Task DesactivarYActivar()
    {
        var categoria = SembrarCategoria();
        var platillo = SembrarPlatillo(categoria.Id);
        var handler = new CambiarEstadoPlatilloHandler(_categorias, _platillos);

        var apagado = await handler.Handle(new CambiarEstadoPlatilloCommand(platillo.Id, false), CancellationToken.None);
        Assert.False(platillo.Activo);
        Assert.False(apagado.Activo);

        var encendido = await handler.Handle(new CambiarEstadoPlatilloCommand(platillo.Id, true), CancellationToken.None);
        Assert.True(platillo.Activo);
        Assert.True(encendido.Activo);
        Assert.Equal(2, _platillos.GuardadosLlamados);
    }

    [Fact(DisplayName = "006/RF-08: sin activo el validador da El estado es obligatorio")]
    public void SinActivoElValidadorLoRechaza()
    {
        var resultado = new CambiarEstadoPlatilloValidator().Validate(new CambiarEstadoPlatilloCommand(Guid.NewGuid(), null));

        var error = Assert.Single(resultado.Errors);
        Assert.Equal("Activo", error.PropertyName);
        Assert.Equal("El estado es obligatorio", error.ErrorMessage);
    }

    [Fact(DisplayName = "006/RF-10: cambiar el estado de un platillo no disponible lanza no encontrado")]
    public async Task CambiarEstadoNoDisponibleLanzaNoEncontrado()
    {
        var categoria = SembrarCategoria();
        var eliminado = SembrarPlatillo(categoria.Id, "Borrado");
        eliminado.Eliminar(_reloj.Ahora);
        var handler = new CambiarEstadoPlatilloHandler(_categorias, _platillos);

        foreach (var id in new[] { Guid.NewGuid(), eliminado.Id })
        {
            await Assert.ThrowsAsync<NoEncontradoException>(() =>
                handler.Handle(new CambiarEstadoPlatilloCommand(id, false), CancellationToken.None));
        }
    }
}
