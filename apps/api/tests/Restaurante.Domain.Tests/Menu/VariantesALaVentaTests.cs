using Restaurante.Domain.Menu;

namespace Restaurante.Domain.Tests.Menu;

public class VariantesALaVentaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid RestauranteId = Guid.NewGuid();

    private static Categoria CategoriaDe(bool activa)
    {
        var c = Categoria.Crear(RestauranteId, new DatosCategoria("Bebidas", null, 1), Ahora);
        if (!activa)
        {
            c.Desactivar();
        }

        return c;
    }

    private static Platillo PlatilloDe(Categoria c, bool activo, bool eliminado)
    {
        var p = Platillo.Crear(RestauranteId, c.Id, new DatosPlatillo("Agua", null, null, 1, []), Ahora);
        if (!activo)
        {
            p.Desactivar();
        }

        if (eliminado)
        {
            p.Eliminar(Ahora);
        }

        return p;
    }

    private static Variante V(Platillo p, string nombre, decimal precio, bool activa = true, bool eliminada = false)
    {
        var v = Variante.Crear(RestauranteId, p.Id, new DatosVariante(nombre, precio, null), Ahora);
        if (!activa)
        {
            v.Desactivar();
        }

        if (eliminada)
        {
            v.Eliminar(Ahora);
        }

        return v;
    }

    [Theory(DisplayName = "007/RF-14: calcula las variantes a la venta")]
    [InlineData(true, true, false, 2)]
    [InlineData(true, false, false, 0)]
    [InlineData(true, true, true, 0)]
    [InlineData(false, true, false, 0)]
    public void De_SegunEstadoDeCategoriaYPlatillo(bool categoriaActiva, bool platilloActivo, bool platilloEliminado, int esperadas)
    {
        var c = CategoriaDe(categoriaActiva);
        var p = PlatilloDe(c, platilloActivo, platilloEliminado);
        var variantes = new[]
        {
            V(p, "Grande", 60m),
            V(p, "Chica", 30m),
            V(p, "Inactiva", 10m, activa: false),
            V(p, "Eliminada", 5m, eliminada: true),
        };

        var resultado = VariantesALaVenta.De(c, p, variantes);

        Assert.Equal(esperadas, resultado.Count);
        if (esperadas > 0)
        {
            Assert.Equal(["Chica", "Grande"], resultado.Select(v => v.Nombre));
        }
    }

    [Fact(DisplayName = "007/RF-14: ordena por precio, nombre e id y descarta variantes de otro platillo")]
    public void De_OrdenaYDescartaAjenas()
    {
        var c = CategoriaDe(true);
        var p = PlatilloDe(c, true, false);
        var otro = PlatilloDe(c, true, false);
        var b = V(p, "b", 10m);
        var a = V(p, "a", 10m);
        var barata = V(p, "z", 5m);
        var ajena = V(otro, "ajena", 1m);

        var resultado = VariantesALaVenta.De(c, p, [b, a, barata, ajena]);

        Assert.Equal([barata, a, b], resultado);
    }

    [Fact(DisplayName = "007/RF-14: a igual precio y nombre desempata por id")]
    public void De_DesempataPorId()
    {
        var c = CategoriaDe(true);
        var p = PlatilloDe(c, true, false);
        var x = V(p, "igual", 10m);
        var y = V(p, "igual", 10m);

        var resultado = VariantesALaVenta.De(c, p, [x, y]);

        var esperado = new[] { x, y }.OrderBy(v => v.Id).ToList();
        Assert.Equal(esperado, resultado);
    }
}
