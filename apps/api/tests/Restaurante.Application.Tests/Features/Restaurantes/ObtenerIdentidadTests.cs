using Restaurante.Application.Features.Restaurantes;
using Restaurante.Application.Features.Restaurantes.ObtenerIdentidad;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Restaurantes;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Application.Tests.Features.Restaurantes;

public class ObtenerIdentidadTests
{
    private readonly RestauranteRepositoryEnMemoria _restaurantes = new();
    private readonly RelojFijo _reloj = new();

    private RestauranteEntidad Sembrar()
    {
        var restaurante = RestauranteEntidad.Crear(
            "La Esquina", "la-esquina", new Paleta("#112233", null, null, null, null), _reloj.Ahora);
        _restaurantes.Agregar(restaurante);
        return restaurante;
    }

    [Fact(DisplayName = "002/RF-14: identidad devuelve nombre, paleta y logoUrl con el etag")]
    public async Task Devuelve_identidad_con_logo()
    {
        var restaurante = Sembrar();
        _restaurantes.AgregarLogo(LogoRestaurante.Crear(restaurante.Id, [1, 2, 3], "image/png", "abc", _reloj.Ahora));
        var handler = new ObtenerIdentidadHandler(_restaurantes, new ContextoRestauranteFalso(restaurante.Id));

        var identidad = await handler.Handle(new ObtenerIdentidadQuery(), CancellationToken.None);

        Assert.Equal("La Esquina", identidad.Nombre);
        Assert.Equal("la-esquina", identidad.Slug);
        Assert.Equal("#112233", identidad.Paleta.Principal);
        Assert.Equal("/api/r/la-esquina/logo?v=abc", identidad.LogoUrl);
    }

    [Fact(DisplayName = "002/RF-14: identidad sin logo devuelve logoUrl nulo")]
    public async Task Sin_logo_logoUrl_es_nulo()
    {
        var restaurante = Sembrar();
        var handler = new ObtenerIdentidadHandler(_restaurantes, new ContextoRestauranteFalso(restaurante.Id));

        var identidad = await handler.Handle(new ObtenerIdentidadQuery(), CancellationToken.None);

        Assert.Null(identidad.LogoUrl);
    }

    [Fact(DisplayName = "002/RF-14: identidad sin contexto de restaurante es un error de programación")]
    public async Task Sin_contexto_lanza()
    {
        var handler = new ObtenerIdentidadHandler(_restaurantes, new ContextoRestauranteFalso(null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ObtenerIdentidadQuery(), CancellationToken.None));
    }
}
