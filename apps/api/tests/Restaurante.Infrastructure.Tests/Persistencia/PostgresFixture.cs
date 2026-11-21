using Microsoft.EntityFrameworkCore;
using Restaurante.Infrastructure.Persistencia;
using Testcontainers.PostgreSql;

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

    public static RestauranteDbContext CrearContexto(string cadena) =>
        new(new DbContextOptionsBuilder<RestauranteDbContext>().UseNpgsql(cadena).Options);
}

[CollectionDefinition(Nombre)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Nombre = "postgres";
}
