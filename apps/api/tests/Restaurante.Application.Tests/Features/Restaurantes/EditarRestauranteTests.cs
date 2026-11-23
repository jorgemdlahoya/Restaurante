using FluentValidation;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Restaurantes;
using Restaurante.Application.Features.Restaurantes.CambiarEstadoRestaurante;
using Restaurante.Application.Features.Restaurantes.EditarRestaurante;
using Restaurante.Application.Features.Restaurantes.ListarRestaurantes;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Restaurantes;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Application.Tests.Features.Restaurantes;

public class EditarRestauranteTests
{
    private readonly RestauranteRepositoryEnMemoria _restaurantes = new();
    private readonly RelojFijo _reloj = new();

    private RestauranteEntidad Sembrar(string nombre, string slug, Paleta? paleta = null)
    {
        var restaurante = RestauranteEntidad.Crear(nombre, slug, paleta ?? Paleta.Vacia, _reloj.Ahora);
        _restaurantes.Agregar(restaurante);
        return restaurante;
    }

    [Fact(DisplayName = "002/RF-03: listar devuelve todos los restaurantes con su estado y paleta")]
    public async Task ListaTodos()
    {
        Sembrar("Zeta", "zeta");
        var con = Sembrar("Alfa", "alfa", new Paleta("#112233", null, null, null, null));
        con.Desactivar();

        var lista = await new ListarRestaurantesHandler(_restaurantes)
            .Handle(new ListarRestaurantesQuery(), CancellationToken.None);

        Assert.Equal(["Alfa", "Zeta"], lista.Select(r => r.Nombre));
        Assert.False(lista[0].Activo);
        Assert.Equal("#112233", lista[0].Paleta.Principal);
        Assert.Null(lista[1].Paleta.Principal);
    }

    [Fact(DisplayName = "002/RF-04: editar cambia nombre y paleta, conserva el slug y guarda una vez")]
    public async Task EditaNombreYPaleta()
    {
        var r = Sembrar("Viejo", "viejo");

        var dto = await new EditarRestauranteHandler(_restaurantes).Handle(
            new EditarRestauranteCommand(r.Id, "  Nuevo ", new PaletaDto("#ABCDEF", null, null, null, null)),
            CancellationToken.None);

        Assert.Equal("Nuevo", dto.Nombre);
        Assert.Equal("viejo", dto.Slug);
        Assert.Equal("#ABCDEF", r.Paleta.Principal);
        Assert.Equal(1, _restaurantes.GuardadosLlamados);
    }

    [Fact(DisplayName = "002/RF-04: editar sin paleta deja la paleta vacía")]
    public async Task SinPaletaLaVacia()
    {
        var r = Sembrar("Viejo", "viejo", new Paleta("#112233", null, null, null, null));

        var dto = await new EditarRestauranteHandler(_restaurantes).Handle(
            new EditarRestauranteCommand(r.Id, "Viejo", null), CancellationToken.None);

        Assert.Null(dto.Paleta.Principal);
    }

    [Fact(DisplayName = "002/RF-04: editar un id inexistente lanza NoEncontradoException")]
    public async Task IdInexistente()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() =>
            new EditarRestauranteHandler(_restaurantes).Handle(
                new EditarRestauranteCommand(Guid.NewGuid(), "X", null), CancellationToken.None));

        Assert.Equal("Restaurante no encontrado", ex.Message);
    }

    [Fact(DisplayName = "002/RF-04: un nombre de otro restaurante lanza ConflictoException y el propio no")]
    public async Task NombreAjeno()
    {
        var propio = Sembrar("Propio", "propio");
        Sembrar("Ajeno", "ajeno");
        var handler = new EditarRestauranteHandler(_restaurantes);

        var ex = await Assert.ThrowsAsync<ConflictoException>(() =>
            handler.Handle(new EditarRestauranteCommand(propio.Id, " Ajeno ", null), CancellationToken.None));
        Assert.Equal("Ya existe un restaurante con ese nombre", ex.Message);
        Assert.Equal(0, _restaurantes.GuardadosLlamados);

        var dto = await handler.Handle(new EditarRestauranteCommand(propio.Id, "Propio", null), CancellationToken.None);
        Assert.Equal("Propio", dto.Nombre);
    }

    [Fact(DisplayName = "002/RF-04: el validador exige nombre de 1 a 80 caracteres y colores válidos")]
    public async Task Validador()
    {
        var validador = new EditarRestauranteValidator();

        var vacio = await validador.ValidateAsync(new EditarRestauranteCommand(Guid.NewGuid(), " ", null));
        var largo = await validador.ValidateAsync(new EditarRestauranteCommand(Guid.NewGuid(), new string('a', 81), null));
        var color = await validador.ValidateAsync(
            new EditarRestauranteCommand(Guid.NewGuid(), "Ok", new PaletaDto("rojo", null, null, null, null)));
        var bien = await validador.ValidateAsync(new EditarRestauranteCommand(Guid.NewGuid(), "Ok", null));

        Assert.Contains(vacio.Errors, e => e.ErrorMessage == "El nombre es obligatorio");
        Assert.Contains(largo.Errors, e => e.ErrorMessage == "El nombre no puede superar 80 caracteres");
        Assert.Contains(color.Errors, e => e.ErrorMessage == ValidadorPaleta.MensajeColor);
        Assert.True(bien.IsValid);
    }

    [Fact(DisplayName = "002/RF-06: cambiar estado desactiva y reactiva sin perder datos")]
    public async Task CambiaEstado()
    {
        var r = Sembrar("Resto", "resto", new Paleta("#112233", null, null, null, null));
        var handler = new CambiarEstadoRestauranteHandler(_restaurantes);

        var inactivo = await handler.Handle(new CambiarEstadoRestauranteCommand(r.Id, false), CancellationToken.None);
        Assert.False(inactivo.Activo);
        var activo = await handler.Handle(new CambiarEstadoRestauranteCommand(r.Id, true), CancellationToken.None);

        Assert.True(activo.Activo);
        Assert.Equal("Resto", activo.Nombre);
        Assert.Equal("resto", activo.Slug);
        Assert.Equal("#112233", activo.Paleta.Principal);
        Assert.Equal(2, _restaurantes.GuardadosLlamados);
    }

    [Fact(DisplayName = "002/RF-06: cambiar estado de un id inexistente lanza NoEncontradoException")]
    public async Task EstadoInexistente()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            new CambiarEstadoRestauranteHandler(_restaurantes).Handle(
                new CambiarEstadoRestauranteCommand(Guid.NewGuid(), true), CancellationToken.None));
    }

    [Fact(DisplayName = "002/RF-06: el validador exige el estado")]
    public async Task ValidadorEstado()
    {
        var resultado = await new CambiarEstadoRestauranteValidator()
            .ValidateAsync(new CambiarEstadoRestauranteCommand(Guid.NewGuid(), null));

        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "El estado es obligatorio");
    }
}
