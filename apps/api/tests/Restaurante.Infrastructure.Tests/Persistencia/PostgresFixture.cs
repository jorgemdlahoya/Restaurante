using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Restaurantes;
using Restaurante.Domain.Sucursales;
using Restaurante.Infrastructure.Persistencia;
using Testcontainers.PostgreSql;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Infrastructure.Tests.Persistencia;

/// <summary>Contenedor PostgreSQL 16 compartido por las pruebas de integración.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder("postgres:16").Build();

    public string CadenaConexion => _contenedor.GetConnectionString();

    public Task InitializeAsync() => _contenedor.StartAsync();

    public Task DisposeAsync() => _contenedor.DisposeAsync().AsTask();

    /// <summary>Crea una base nueva y vacía para aislar cada prueba.</summary>
    public async Task<string> CrearBaseAsync()
    {
        var nombre = "t_" + Guid.NewGuid().ToString("N");
        await using (var admin = new Npgsql.NpgsqlConnection(CadenaConexion))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE {nombre}";
            await cmd.ExecuteNonQueryAsync();
        }

        return new Npgsql.NpgsqlConnectionStringBuilder(CadenaConexion) { Database = nombre }.ConnectionString;
    }

    /// <summary>
    /// Contexto con el filtro global del restaurante y la sucursal indicados; restaurante <c>null</c> es el
    /// ámbito de plataforma y sucursal <c>null</c> no ve ninguna entidad de sucursal.
    /// </summary>
    public static RestauranteDbContext CrearContexto(string cadena, Guid? restauranteId = null, Guid? sucursalId = null) =>
        new(new DbContextOptionsBuilder<RestauranteDbContext>().UseNpgsql(cadena).Options,
            new ContextoRestauranteFijo(restauranteId),
            new ContextoSucursalFijo(sucursalId));

    /// <summary>Inserta un restaurante (base ya migrada) y devuelve su id.</summary>
    public static async Task<Guid> SembrarRestauranteAsync(string cadena, string slug)
    {
        await using var ctx = CrearContexto(cadena);
        var restaurante = RestauranteEntidad.Crear("Restaurante " + slug, slug, Paleta.Vacia,
            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        ctx.Restaurantes.Add(restaurante);
        await ctx.SaveChangesAsync();
        return restaurante.Id;
    }

    /// <summary>
    /// Siembra una sucursal "centro" del restaurante y devuelve su id, para los usuarios que por la cardinalidad
    /// de 004/RF-02 necesitan una (cajero, cocinero, administrador de sucursal).
    /// </summary>
    public static async Task<Guid> SembrarSucursalAsync(string cadena, Guid restauranteId, string whatsApp = "+525500009999")
    {
        await using var ctx = CrearContexto(cadena, restauranteId);
        var sucursal = Sucursal.Crear(
            restauranteId,
            "centro",
            new DatosSucursal("Centro", "Calle 1", "+525511111111", whatsApp, null, "C", ZonasHorarias.PorDefecto),
            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var repo = new SucursalRepository(ctx);
        repo.Agregar(sucursal, HorarioDia.SemanaCerrada(restauranteId, sucursal.Id));
        await repo.GuardarCambiosAsync(CancellationToken.None);
        return sucursal.Id;
    }
}

[CollectionDefinition(Nombre)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Nombre = "postgres";
}
