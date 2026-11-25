using Npgsql;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Menu;
using Restaurante.Infrastructure.Persistencia;

namespace Restaurante.Infrastructure.Tests.Persistencia;

[Trait("Category", "Integracion")]
[Collection(PostgresCollection.Nombre)]
public sealed class CategoriasIntegracionTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Ahora = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private async Task<string> BaseMigradaAsync()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var ctx = PostgresFixture.CrearContexto(cadena);
        await new AplicadorMigraciones(ctx).AplicarAsync(true, CancellationToken.None);
        return cadena;
    }

    private static async Task GuardarAsync(string cadena, Guid restauranteId, Categoria categoria)
    {
        await using var ctx = PostgresFixture.CrearContexto(cadena, restauranteId);
        var repo = new CategoriaRepository(ctx);
        repo.Agregar(categoria);
        await repo.GuardarCambiosAsync(CancellationToken.None);
    }

    private static Categoria Nueva(Guid restauranteId, string nombre, string? corto = null, int orden = 1) =>
        Categoria.Crear(restauranteId, new DatosCategoria(nombre, corto, orden), Ahora);

    [Fact(DisplayName = "005/RF-07: una categoría eliminada no se ve y libera su nombre")]
    public async Task Eliminada_no_se_ve_y_libera_nombre()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var bebidas = Nueva(r, "Bebidas");
        await GuardarAsync(cadena, r, bebidas);

        await using (var ctx = PostgresFixture.CrearContexto(cadena, r))
        {
            var repo = new CategoriaRepository(ctx);
            var cargada = await repo.ObtenerPorIdAsync(bebidas.Id, CancellationToken.None);
            Assert.NotNull(cargada);
            cargada.Eliminar(Ahora.AddDays(1));
            await repo.GuardarCambiosAsync(CancellationToken.None);
        }

        await using (var ctx = PostgresFixture.CrearContexto(cadena, r))
        {
            var repo = new CategoriaRepository(ctx);
            Assert.Null(await repo.ObtenerPorIdAsync(bebidas.Id, CancellationToken.None));
            var (elementos, total) = await repo.ListarAsync(1, 10, CancellationToken.None);
            Assert.Empty(elementos);
            Assert.Equal(0, total);
            Assert.False(await repo.ExisteNombreAsync("bebidas", null, CancellationToken.None));
        }

        await GuardarAsync(cadena, r, Nueva(r, "Bebidas"));
    }

    [Fact(DisplayName = "005/RF-03: los índices únicos traducen a ConflictoCamposException por campo")]
    public async Task Indices_unicos_traducen_a_conflicto_por_campo()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        await GuardarAsync(cadena, r, Nueva(r, "Bebidas", "Bebs"));

        var porNombre = await Assert.ThrowsAsync<ConflictoCamposException>(
            () => GuardarAsync(cadena, r, Nueva(r, "BEBIDAS", "Otra")));
        Assert.Equal(["Ya existe una categoría con ese nombre"], porNombre.Errores["nombre"]);
        Assert.False(porNombre.Errores.ContainsKey("nombreCorto"));

        var porCorto = await Assert.ThrowsAsync<ConflictoCamposException>(
            () => GuardarAsync(cadena, r, Nueva(r, "Postres", "bebs")));
        Assert.Equal(["Ya existe una categoría con ese nombre corto"], porCorto.Errores["nombreCorto"]);
        Assert.Equal("Ya existe una categoría con ese nombre corto", porCorto.Message);

        await GuardarAsync(cadena, r, Nueva(r, "Entradas"));
        await GuardarAsync(cadena, r, Nueva(r, "Sopas"));
    }

    [Fact(DisplayName = "005/RF-08: las categorías de otro restaurante no se ven")]
    public async Task Categorias_de_otro_restaurante_no_se_ven()
    {
        var cadena = await BaseMigradaAsync();
        var r1 = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var r2 = await PostgresFixture.SembrarRestauranteAsync(cadena, "beta");
        var categoria = Nueva(r1, "Bebidas");
        await GuardarAsync(cadena, r1, categoria);

        await using var ctx = PostgresFixture.CrearContexto(cadena, r2);
        var repo = new CategoriaRepository(ctx);
        Assert.Null(await repo.ObtenerPorIdAsync(categoria.Id, CancellationToken.None));
        var (elementos, total) = await repo.ListarAsync(1, 10, CancellationToken.None);
        Assert.Empty(elementos);
        Assert.Equal(0, total);
        Assert.False(await repo.ExisteNombreAsync("bebidas", null, CancellationToken.None));
    }

    [Fact(DisplayName = "005/RF-04: la lista pagina y ordena por orden y nombre")]
    public async Task Lista_pagina_y_ordena()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        await GuardarAsync(cadena, r, Nueva(r, "B", orden: 2));
        await GuardarAsync(cadena, r, Nueva(r, "Z", orden: 1));
        await GuardarAsync(cadena, r, Nueva(r, "A", orden: 1));

        await using var ctx = PostgresFixture.CrearContexto(cadena, r);
        var repo = new CategoriaRepository(ctx);
        var (primera, total) = await repo.ListarAsync(1, 2, CancellationToken.None);
        Assert.Equal(["A", "Z"], primera.Select(c => c.Nombre));
        Assert.Equal(3, total);
        var (segunda, total2) = await repo.ListarAsync(2, 2, CancellationToken.None);
        Assert.Equal(["B"], segunda.Select(c => c.Nombre));
        Assert.Equal(3, total2);
    }

    [Fact(DisplayName = "005/RF-02: la base rechaza orden menor que 1")]
    public async Task Base_rechaza_orden_menor_que_uno()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");

        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var cmd = conexion.CreateCommand();
        cmd.CommandText =
            "INSERT INTO categorias (id, restaurante_id, nombre, nombre_normalizado, orden, activa, creada_en) " +
            "VALUES (@id, @r, 'X', 'x', 0, true, now())";
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("r", r);
        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("ck_categorias_orden", ex.ConstraintName);
    }
}
