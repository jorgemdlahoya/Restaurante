using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;
using Restaurante.Infrastructure.Persistencia;

namespace Restaurante.Infrastructure.Tests.Persistencia;

[Trait("Category", "Integracion")]
[Collection(PostgresCollection.Nombre)]
public sealed class UsuariosSucursalesTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Ahora = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private async Task<string> BaseMigradaAsync()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var ctx = PostgresFixture.CrearContexto(cadena);
        await new AplicadorMigraciones(ctx).AplicarAsync(true, CancellationToken.None);
        return cadena;
    }

    private static async Task<Sucursal> SembrarSucursalAsync(string cadena, Guid restauranteId, string slug, string prefijo)
    {
        await using var ctx = PostgresFixture.CrearContexto(cadena, restauranteId);
        var datos = new DatosSucursal(
            slug, "Calle 1", "+525511111111", $"+52550000{(int)prefijo[0]:D4}", null, prefijo, ZonasHorarias.PorDefecto);
        var sucursal = Sucursal.Crear(restauranteId, slug, datos, Ahora);
        var repo = new SucursalRepository(ctx);
        repo.Agregar(sucursal, HorarioDia.SemanaCerrada(restauranteId, sucursal.Id));
        await repo.GuardarCambiosAsync(CancellationToken.None);
        return sucursal;
    }

    private static async Task<Usuario> SembrarUsuarioAsync(
        string cadena, Guid restauranteId, string nombre, Rol rol, params Guid[] sucursales)
    {
        await using var ctx = PostgresFixture.CrearContexto(cadena, restauranteId);
        var usuario = Usuario.Crear(restauranteId, nombre, "hash", rol, sucursales, Ahora);
        var repo = new UsuarioRepository(ctx);
        repo.Agregar(usuario);
        await repo.GuardarCambiosAsync(CancellationToken.None);
        return usuario;
    }

    [Fact(DisplayName = "004/RF-01: las sucursales del usuario se guardan y se leen")]
    public async Task Las_sucursales_se_guardan_y_se_leen()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var centro = await SembrarSucursalAsync(cadena, r, "centro", "C");
        var norte = await SembrarSucursalAsync(cadena, r, "norte", "N");
        var usuario = await SembrarUsuarioAsync(cadena, r, "Ana", Rol.Cajero, centro.Id, norte.Id);

        await using var ctx = PostgresFixture.CrearContexto(cadena, r);
        var repo = new UsuarioRepository(ctx);

        var porId = await repo.ObtenerPorIdAsync(usuario.Id, CancellationToken.None);
        var porNombre = await repo.ObtenerPorNombreNormalizadoAsync("ana", CancellationToken.None);

        Assert.NotNull(porId);
        Assert.Equal([centro.Id, norte.Id], porId.Sucursales.Order());
        Assert.NotNull(porNombre);
        Assert.Equal([centro.Id, norte.Id], porNombre.Sucursales.Order());
        Assert.Equal(usuario.SelloSesion, porId.SelloSesion);
    }

    [Fact(DisplayName = "004/RF-01: ListarAsync ordena por nombre normalizado y trae las sucursales")]
    public async Task Listar_ordena_por_nombre_y_trae_sucursales()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var centro = await SembrarSucursalAsync(cadena, r, "centro", "C");
        await SembrarUsuarioAsync(cadena, r, "Zoe", Rol.Cajero, centro.Id);
        await SembrarUsuarioAsync(cadena, r, "ana", Rol.Cocinero, centro.Id);

        await using var ctx = PostgresFixture.CrearContexto(cadena, r);
        var lista = await new UsuarioRepository(ctx).ListarAsync(CancellationToken.None);

        Assert.Equal(["ana", "zoe"], lista.Select(u => u.NombreUsuarioNormalizado));
        Assert.All(lista, u => Assert.Equal([centro.Id], u.Sucursales));
    }

    [Fact(DisplayName = "004/RF-01: no se puede asignar una sucursal inexistente")]
    public async Task No_se_asigna_una_sucursal_inexistente()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");

        var ex = await Assert.ThrowsAsync<DbUpdateException>(
            () => SembrarUsuarioAsync(cadena, r, "Ana", Rol.Cajero, Guid.NewGuid()));

        Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ((PostgresException)ex.InnerException).SqlState);
    }

    [Fact(DisplayName = "004/RF-13: ObtenerParaSesionAsync ignora el filtro de restaurante")]
    public async Task Obtener_para_sesion_ignora_el_filtro_de_restaurante()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var centro = await SembrarSucursalAsync(cadena, r, "centro", "C");
        var usuario = await SembrarUsuarioAsync(cadena, r, "Ana", Rol.Cajero, centro.Id);

        // Contexto sin restaurante: el filtro global oculta al usuario, la consulta de sesión no.
        await using var ctx = PostgresFixture.CrearContexto(cadena);
        var repo = new UsuarioRepository(ctx);
        Assert.Null(await repo.ObtenerPorIdAsync(usuario.Id, CancellationToken.None));

        var deSesion = await repo.ObtenerParaSesionAsync(usuario.Id, CancellationToken.None);

        Assert.NotNull(deSesion);
        Assert.Equal(usuario.SelloSesion, deSesion.SelloSesion);
        Assert.Equal([centro.Id], deSesion.Sucursales);
        Assert.Equal(EntityState.Detached, ctx.Entry(deSesion).State);
        Assert.Null(await repo.ObtenerParaSesionAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact(DisplayName = "004/RF-13: la migración da un sello distinto a cada usuario existente")]
    public async Task La_migracion_da_un_sello_distinto_a_cada_usuario()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var ctx = PostgresFixture.CrearContexto(cadena);
        var migrator = ctx.GetService<IMigrator>();
        var migraciones = ctx.Database.GetMigrations().ToList();
        var destino = migraciones.Single(m => m.EndsWith("_UsuariosSucursales", StringComparison.Ordinal));
        var anterior = migraciones[migraciones.IndexOf(destino) - 1];

        await migrator.MigrateAsync(anterior);
        await ctx.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO usuarios (id, restaurante_id, nombre_usuario, nombre_usuario_normalizado, hash_contrasena, rol, activo, creado_en)
            VALUES ({0}, NULL, 'Uno', 'uno', 'h', 'administrador_plataforma', true, now()),
                   ({1}, NULL, 'Dos', 'dos', 'h', 'administrador_plataforma', true, now())
            """,
            Guid.NewGuid(), Guid.NewGuid());

        await migrator.MigrateAsync(destino);

        var sellos = new List<Guid>();
        await using (var conexion = new NpgsqlConnection(cadena))
        {
            await conexion.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT sello_sesion FROM usuarios", conexion);
            await using var lector = await cmd.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                sellos.Add(lector.GetGuid(0));
            }
        }

        Assert.Equal(2, sellos.Count);
        Assert.DoesNotContain(Guid.Empty, sellos);
        Assert.Equal(2, sellos.Distinct().Count());
    }

    [Fact(DisplayName = "004/RF-13: la consulta de sesión tarda menos de 10 ms de mediana")]
    public async Task La_consulta_de_sesion_tarda_menos_de_10_ms()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var centro = await SembrarSucursalAsync(cadena, r, "centro", "C");
        var norte = await SembrarSucursalAsync(cadena, r, "norte", "N");
        var usuario = await SembrarUsuarioAsync(cadena, r, "Ana", Rol.Cajero, centro.Id, norte.Id);

        // Un contexto nuevo por lectura, como en cada petición.
        async Task LeerAsync()
        {
            await using var ctx = PostgresFixture.CrearContexto(cadena);
            Assert.NotNull(await new UsuarioRepository(ctx).ObtenerParaSesionAsync(usuario.Id, CancellationToken.None));
        }

        for (var i = 0; i < 10; i++)
        {
            await LeerAsync();
        }

        var tiempos = new List<double>();
        for (var i = 0; i < 100; i++)
        {
            var inicio = Stopwatch.GetTimestamp();
            await LeerAsync();
            tiempos.Add(Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
        }

        tiempos.Sort();
        var mediana = (tiempos[49] + tiempos[50]) / 2;
        Assert.True(mediana < 10, $"Mediana {mediana:F2} ms (RNF-01: < 10 ms).");
    }
}
