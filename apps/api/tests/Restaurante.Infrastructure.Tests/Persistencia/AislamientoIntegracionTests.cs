using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;
using Restaurante.Infrastructure.Persistencia;

namespace Restaurante.Infrastructure.Tests.Persistencia;

[Trait("Category", "Integracion")]
[Collection(PostgresCollection.Nombre)]
public sealed class AislamientoIntegracionTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Ahora = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private async Task<(string Cadena, Guid A, Guid B)> BaseConDosRestaurantesAsync()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using (var ctx = PostgresFixture.CrearContexto(cadena))
        {
            await new AplicadorMigraciones(ctx).AplicarAsync(true, CancellationToken.None);
        }

        var a = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var b = await PostgresFixture.SembrarRestauranteAsync(cadena, "beta");
        return (cadena, a, b);
    }

    [Fact(DisplayName = "002/RF-12: el filtro global sólo devuelve usuarios del restaurante del contexto")]
    public async Task Filtro_global_aisla_por_restaurante()
    {
        var (cadena, a, b) = await BaseConDosRestaurantesAsync();
        var plataforma = Usuario.Crear(null, "root", "h", Rol.AdministradorPlataforma, [], Ahora);
        var deA = Usuario.Crear(a, "ana", "h", Rol.Cajero, [], Ahora);
        var deB = Usuario.Crear(b, "beto", "h", Rol.Cocinero, [], Ahora);
        await using (var ctx = PostgresFixture.CrearContexto(cadena))
        {
            ctx.Usuarios.AddRange(plataforma, deA, deB);
            await ctx.SaveChangesAsync();
        }

        await using (var ctxA = PostgresFixture.CrearContexto(cadena, a))
        {
            var repoA = new UsuarioRepository(ctxA);
            var listaA = await repoA.ListarAsync(CancellationToken.None);
            Assert.Equal(["ana"], listaA.Select(u => u.NombreUsuario));
            Assert.Null(await repoA.ObtenerPorIdAsync(deB.Id, CancellationToken.None));
            Assert.Null(await repoA.ObtenerPorIdAsync(plataforma.Id, CancellationToken.None));
            Assert.NotNull(await repoA.ObtenerPorIdAsync(deA.Id, CancellationToken.None));
            Assert.Null(await repoA.ObtenerPorNombreNormalizadoAsync("beto", CancellationToken.None));
        }

        await using var ctxPlataforma = PostgresFixture.CrearContexto(cadena);
        var repoPlataforma = new UsuarioRepository(ctxPlataforma);
        var lista = await repoPlataforma.ListarAsync(CancellationToken.None);
        Assert.Equal(["root"], lista.Select(u => u.NombreUsuario));
        Assert.Null(await repoPlataforma.ObtenerPorIdAsync(deA.Id, CancellationToken.None));
        Assert.True(await repoPlataforma.ExisteAlgunoAsync(CancellationToken.None));
    }

    [Fact(DisplayName = "002/RF-13: el índice permite el mismo nombre en dos restaurantes y lo impide en uno")]
    public async Task Indice_unico_por_restaurante()
    {
        var (cadena, a, b) = await BaseConDosRestaurantesAsync();
        await using (var ctx = PostgresFixture.CrearContexto(cadena))
        {
            var repo = new UsuarioRepository(ctx);
            repo.Agregar(Usuario.Crear(a, "Maria", "h", Rol.Cajero, [], Ahora));
            repo.Agregar(Usuario.Crear(b, "maria", "h", Rol.Cajero, [], Ahora));
            repo.Agregar(Usuario.Crear(null, "root", "h", Rol.AdministradorPlataforma, [], Ahora));
            await repo.GuardarCambiosAsync(CancellationToken.None);
        }

        await using (var ctx = PostgresFixture.CrearContexto(cadena, a))
        {
            var repo = new UsuarioRepository(ctx);
            repo.Agregar(Usuario.Crear(a, "MARIA", "h", Rol.Cocinero, [], Ahora));
            await Assert.ThrowsAsync<ConflictoException>(() => repo.GuardarCambiosAsync(CancellationToken.None));
        }

        await using (var ctx = PostgresFixture.CrearContexto(cadena))
        {
            var repo = new UsuarioRepository(ctx);
            repo.Agregar(Usuario.Crear(null, "ROOT", "h", Rol.AdministradorPlataforma, [], Ahora));
            await Assert.ThrowsAsync<ConflictoException>(() => repo.GuardarCambiosAsync(CancellationToken.None));
        }
    }
}
