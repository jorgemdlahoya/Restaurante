using Microsoft.EntityFrameworkCore;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Usuarios;
using Restaurante.Infrastructure.Persistencia;

namespace Restaurante.Infrastructure.Tests.Persistencia;

[Trait("Category", "Integracion")]
[Collection(PostgresCollection.Nombre)]
public sealed class PersistenciaIntegracionTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Ahora = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static async Task<bool> ExisteTablaAsync(RestauranteDbContext ctx)
    {
        var conexion = ctx.Database.GetDbConnection();
        await conexion.OpenAsync();
        try
        {
            await using var cmd = conexion.CreateCommand();
            cmd.CommandText = "SELECT to_regclass('public.usuarios') IS NOT NULL";
            return (bool)(await cmd.ExecuteScalarAsync())!;
        }
        finally
        {
            await conexion.CloseAsync();
        }
    }

    [Fact(DisplayName = "001/RF-26: aplicar migraciones crea la tabla usuarios")]
    public async Task Aplicar_migraciones_crea_la_tabla()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var ctx = PostgresFixture.CrearContexto(cadena);

        await new AplicadorMigraciones(ctx).AplicarAsync(true, CancellationToken.None);

        Assert.True(await ExisteTablaAsync(ctx));
    }

    [Fact(DisplayName = "001/RF-26: con aplicar=false no crea la tabla")]
    public async Task Sin_aplicar_no_crea_la_tabla()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var ctx = PostgresFixture.CrearContexto(cadena);

        await new AplicadorMigraciones(ctx).AplicarAsync(false, CancellationToken.None);

        Assert.False(await ExisteTablaAsync(ctx));
    }

    [Fact(DisplayName = "001/RF-12: el índice único no distingue mayúsculas")]
    public async Task Indice_unico_no_distingue_mayusculas()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var ctx = PostgresFixture.CrearContexto(cadena);
        await new AplicadorMigraciones(ctx).AplicarAsync(true, CancellationToken.None);
        var restaurante = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");

        var repo = new UsuarioRepository(ctx);
        repo.Agregar(Usuario.Crear(restaurante, "Ana", "h", Rol.Cajero, [], Ahora));
        await repo.GuardarCambiosAsync(CancellationToken.None);

        await using var ctx2 = PostgresFixture.CrearContexto(cadena, restaurante);
        var repo2 = new UsuarioRepository(ctx2);
        repo2.Agregar(Usuario.Crear(restaurante, "ANA", "h", Rol.Cajero, [], Ahora));
        await Assert.ThrowsAsync<ConflictoException>(() => repo2.GuardarCambiosAsync(CancellationToken.None));

        await using var ctx3 = PostgresFixture.CrearContexto(cadena, restaurante);
        var encontrado = await new UsuarioRepository(ctx3).ObtenerPorNombreNormalizadoAsync("ana", CancellationToken.None);
        Assert.Equal(Rol.Cajero, encontrado!.Rol);
    }

    [Fact(DisplayName = "001/RF-12: alta duplicada concurrente lanza ConflictoException")]
    public async Task Alta_duplicada_concurrente_lanza_conflicto()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using (var migracion = PostgresFixture.CrearContexto(cadena))
        {
            await new AplicadorMigraciones(migracion).AplicarAsync(true, CancellationToken.None);
        }

        var restaurante = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");

        async Task<Exception?> AltaAsync()
        {
            await using var ctx = PostgresFixture.CrearContexto(cadena, restaurante);
            var repo = new UsuarioRepository(ctx);
            repo.Agregar(Usuario.Crear(restaurante, "Luis", "h", Rol.Cocinero, [], Ahora));
            try
            {
                await repo.GuardarCambiosAsync(CancellationToken.None);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        var resultados = await Task.WhenAll(AltaAsync(), AltaAsync());

        Assert.Single(resultados, r => r is null);
        Assert.Single(resultados, r => r is ConflictoException);
    }
}
