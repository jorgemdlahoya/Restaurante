using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Restaurante.Domain.Usuarios;
using Restaurante.Infrastructure.Persistencia;

namespace Restaurante.Infrastructure.Tests.Persistencia;

[Trait("Category", "Integracion")]
[Collection(PostgresCollection.Nombre)]
public sealed class RolesIntegracionTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Ahora = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "002/RF-17: la migración guarda y lee administrador_plataforma")]
    public async Task Guarda_y_lee_administrador_plataforma()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using (var ctx = PostgresFixture.CrearContexto(cadena))
        {
            await new AplicadorMigraciones(ctx).AplicarAsync(true, CancellationToken.None);
            var repo = new UsuarioRepository(ctx);
            repo.Agregar(Usuario.Crear(null, "plataforma", "h", Rol.AdministradorPlataforma, Ahora));
            await repo.GuardarCambiosAsync(CancellationToken.None);
        }

        await using var nuevo = PostgresFixture.CrearContexto(cadena);
        var leido = await nuevo.Usuarios.SingleAsync(u => u.NombreUsuarioNormalizado == "plataforma");
        Assert.Equal(Rol.AdministradorPlataforma, leido.Rol);
        var crudo = await nuevo.Database.SqlQueryRaw<string>("SELECT rol AS \"Value\" FROM usuarios").SingleAsync();
        Assert.Equal("administrador_plataforma", crudo);
    }

    [Fact(DisplayName = "002/RF-17: la migración RolesPlataforma vacía la tabla usuarios")]
    public async Task Migracion_vacia_la_tabla_usuarios()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var ctx = PostgresFixture.CrearContexto(cadena);
        var migrador = ctx.GetService<IMigrator>();
        await migrador.MigrateAsync("Inicial");

        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO usuarios (id, nombre_usuario, nombre_usuario_normalizado, hash_contrasena, rol, activo, creado_en) " +
            "VALUES (gen_random_uuid(), 'Viejo', 'viejo', 'h', 'administrador', true, now())");

        await migrador.MigrateAsync();

        var total = await ctx.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM usuarios").SingleAsync();
        Assert.Equal(0, total);
    }
}
