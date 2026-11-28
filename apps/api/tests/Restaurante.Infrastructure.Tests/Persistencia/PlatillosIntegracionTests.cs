using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Menu;
using Restaurante.Infrastructure.Persistencia;

namespace Restaurante.Infrastructure.Tests.Persistencia;

[Trait("Category", "Integracion")]
[Collection(PostgresCollection.Nombre)]
public sealed class PlatillosIntegracionTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Ahora = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private async Task<string> BaseMigradaAsync()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var ctx = PostgresFixture.CrearContexto(cadena);
        await new AplicadorMigraciones(ctx).AplicarAsync(true, CancellationToken.None);
        return cadena;
    }

    private static async Task<Guid> SembrarCategoriaAsync(string cadena, Guid restauranteId, string nombre)
    {
        await using var ctx = PostgresFixture.CrearContexto(cadena, restauranteId);
        var repo = new CategoriaRepository(ctx);
        var categoria = Categoria.Crear(restauranteId, new DatosCategoria(nombre, null, 1), Ahora);
        repo.Agregar(categoria);
        await repo.GuardarCambiosAsync(CancellationToken.None);
        return categoria.Id;
    }

    private static async Task GuardarAsync(string cadena, Guid restauranteId, Platillo platillo)
    {
        await using var ctx = PostgresFixture.CrearContexto(cadena, restauranteId);
        var repo = new PlatilloRepository(ctx);
        repo.Agregar(platillo);
        await repo.GuardarCambiosAsync(CancellationToken.None);
    }

    private static Platillo Nuevo(
        Guid restauranteId, Guid categoriaId, string nombre, string? corto = null, int orden = 1,
        params string[] ingredientes) =>
        Platillo.Crear(restauranteId, categoriaId, new DatosPlatillo(nombre, corto, null, orden, ingredientes), Ahora);

    [Fact(DisplayName = "006/RF-05: los ingredientes se guardan en su orden y la lista pagina por orden y nombre")]
    public async Task Ingredientes_en_orden_y_lista_pagina_por_orden_y_nombre()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var c = await SembrarCategoriaAsync(cadena, r, "Sopas");
        var otra = await SembrarCategoriaAsync(cadena, r, "Postres");
        var conIngredientes = Nuevo(r, c, "Z", orden: 1, ingredientes: ["Queso", "Crema", "Cebolla"]);
        await GuardarAsync(cadena, r, Nuevo(r, c, "B", orden: 2));
        await GuardarAsync(cadena, r, conIngredientes);
        await GuardarAsync(cadena, r, Nuevo(r, c, "A", orden: 1));
        await GuardarAsync(cadena, r, Nuevo(r, otra, "Flan", orden: 1));

        await using var ctx = PostgresFixture.CrearContexto(cadena, r);
        var repo = new PlatilloRepository(ctx);
        var (primera, total) = await repo.ListarPorCategoriaAsync(c, 1, 2, CancellationToken.None);
        Assert.Equal(["A", "Z"], primera.Select(p => p.Nombre));
        Assert.Equal(3, total);
        Assert.Equal(["Queso", "Crema", "Cebolla"], primera[1].Ingredientes);
        Assert.Empty(primera[0].Ingredientes);
        var (segunda, total2) = await repo.ListarPorCategoriaAsync(c, 2, 2, CancellationToken.None);
        Assert.Equal(["B"], segunda.Select(p => p.Nombre));
        Assert.Equal(3, total2);
        var (enorme, total3) = await repo.ListarPorCategoriaAsync(c, int.MaxValue, 20, CancellationToken.None);
        Assert.Empty(enorme);
        Assert.Equal(3, total3);
    }

    [Fact(DisplayName = "006/RF-05: editar los ingredientes persiste la nueva lista")]
    public async Task Editar_ingredientes_persiste_la_nueva_lista()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var c = await SembrarCategoriaAsync(cadena, r, "Sopas");
        var platillo = Nuevo(r, c, "Pozole", orden: 1, ingredientes: ["Maíz", "Cerdo"]);
        await GuardarAsync(cadena, r, platillo);

        await using (var ctx = PostgresFixture.CrearContexto(cadena, r))
        {
            var repo = new PlatilloRepository(ctx);
            var cargado = await repo.ObtenerPorIdAsync(platillo.Id, CancellationToken.None);
            Assert.NotNull(cargado);
            cargado.Editar(c, new DatosPlatillo("Pozole", null, null, 1, ["Cerdo", "Rábano", "Lechuga"]));
            await repo.GuardarCambiosAsync(CancellationToken.None);
        }

        await using (var ctx = PostgresFixture.CrearContexto(cadena, r))
        {
            var recargado = await new PlatilloRepository(ctx).ObtenerPorIdAsync(platillo.Id, CancellationToken.None);
            Assert.NotNull(recargado);
            Assert.Equal(["Cerdo", "Rábano", "Lechuga"], recargado.Ingredientes);
        }
    }

    [Fact(DisplayName = "006/RF-04: los índices únicos traducen a ConflictoCamposException por campo")]
    public async Task Indices_unicos_traducen_a_conflicto_por_campo()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var c1 = await SembrarCategoriaAsync(cadena, r, "Sopas");
        var c2 = await SembrarCategoriaAsync(cadena, r, "Postres");
        await GuardarAsync(cadena, r, Nuevo(r, c1, "Pozole", "Pozo"));

        var porNombre = await Assert.ThrowsAsync<ConflictoCamposException>(
            () => GuardarAsync(cadena, r, Nuevo(r, c2, "POZOLE", "Otro")));
        Assert.Equal(["Ya existe un platillo con ese nombre"], porNombre.Errores["nombre"]);
        Assert.False(porNombre.Errores.ContainsKey("nombreCorto"));

        var porCorto = await Assert.ThrowsAsync<ConflictoCamposException>(
            () => GuardarAsync(cadena, r, Nuevo(r, c2, "Flan", "pozo")));
        Assert.Equal(["Ya existe un platillo con ese nombre corto"], porCorto.Errores["nombreCorto"]);

        await GuardarAsync(cadena, r, Nuevo(r, c1, "Menudo"));
        await GuardarAsync(cadena, r, Nuevo(r, c1, "Caldo"));
    }

    [Fact(DisplayName = "006/RF-09: un platillo eliminado no se ve, no cuenta y libera su nombre")]
    public async Task Eliminado_no_se_ve_no_cuenta_y_libera_nombre()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var c = await SembrarCategoriaAsync(cadena, r, "Sopas");
        var pozole = Nuevo(r, c, "Pozole", "Pozo");
        await GuardarAsync(cadena, r, pozole);

        await using (var ctx = PostgresFixture.CrearContexto(cadena, r))
        {
            var repo = new PlatilloRepository(ctx);
            var cargado = await repo.ObtenerPorIdAsync(pozole.Id, CancellationToken.None);
            Assert.NotNull(cargado);
            cargado.Eliminar(Ahora.AddDays(1));
            await repo.GuardarCambiosAsync(CancellationToken.None);
        }

        await using (var ctx = PostgresFixture.CrearContexto(cadena, r))
        {
            var repo = new PlatilloRepository(ctx);
            Assert.Null(await repo.ObtenerPorIdAsync(pozole.Id, CancellationToken.None));
            var (elementos, total) = await repo.ListarPorCategoriaAsync(c, 1, 10, CancellationToken.None);
            Assert.Empty(elementos);
            Assert.Equal(0, total);
            Assert.Equal(0, await repo.ContarPorCategoriaAsync(c, CancellationToken.None));
            Assert.False(await repo.ExisteNombreAsync("pozole", null, CancellationToken.None));
            Assert.False(await repo.ExisteNombreCortoAsync("pozo", null, CancellationToken.None));
        }

        await GuardarAsync(cadena, r, Nuevo(r, c, "Pozole", "Pozo"));
    }

    [Fact(DisplayName = "006/RF-10: los platillos de otro restaurante no se ven")]
    public async Task Platillos_de_otro_restaurante_no_se_ven()
    {
        var cadena = await BaseMigradaAsync();
        var r1 = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var r2 = await PostgresFixture.SembrarRestauranteAsync(cadena, "beta");
        var c = await SembrarCategoriaAsync(cadena, r1, "Sopas");
        var platillo = Nuevo(r1, c, "Pozole");
        await GuardarAsync(cadena, r1, platillo);

        await using var ctx = PostgresFixture.CrearContexto(cadena, r2);
        var repo = new PlatilloRepository(ctx);
        Assert.Null(await repo.ObtenerPorIdAsync(platillo.Id, CancellationToken.None));
        var (elementos, total) = await repo.ListarPorCategoriaAsync(c, 1, 10, CancellationToken.None);
        Assert.Empty(elementos);
        Assert.Equal(0, total);
        Assert.Equal(0, await repo.ContarPorCategoriaAsync(c, CancellationToken.None));
        Assert.False(await repo.ExisteNombreAsync("pozole", null, CancellationToken.None));
    }

    [Fact(DisplayName = "006/RF-12: ContarPorCategoriaAsync cuenta activos e inactivos no eliminados")]
    public async Task Contar_por_categoria_cuenta_activos_e_inactivos_no_eliminados()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var c = await SembrarCategoriaAsync(cadena, r, "Sopas");
        var otra = await SembrarCategoriaAsync(cadena, r, "Postres");
        var inactivo = Nuevo(r, c, "Menudo");
        inactivo.Desactivar();
        var eliminado = Nuevo(r, c, "Caldo");
        eliminado.Eliminar(Ahora.AddDays(1));
        await GuardarAsync(cadena, r, Nuevo(r, c, "Pozole"));
        await GuardarAsync(cadena, r, inactivo);
        await GuardarAsync(cadena, r, eliminado);
        await GuardarAsync(cadena, r, Nuevo(r, otra, "Flan"));

        await using var ctx = PostgresFixture.CrearContexto(cadena, r);
        var repo = new PlatilloRepository(ctx);
        Assert.Equal(2, await repo.ContarPorCategoriaAsync(c, CancellationToken.None));
        Assert.Equal(1, await repo.ContarPorCategoriaAsync(otra, CancellationToken.None));
    }
}
