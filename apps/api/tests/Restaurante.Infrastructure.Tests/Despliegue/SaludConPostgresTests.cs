using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Restaurante.Domain.Usuarios;
using Restaurante.Infrastructure.Tests.Persistencia;

namespace Restaurante.Infrastructure.Tests.Despliegue;

/// <summary>
/// Arranca la API real (sin dobles: health check de EF Core, migraciones y administrador inicial)
/// contra el PostgreSQL del contenedor. Requiere Docker.
/// </summary>
[Collection(PostgresCollection.Nombre)]
[Trait("Category", "Integracion")]
public sealed class SaludConPostgresTests(PostgresFixture postgres)
{
    private sealed class ApiReal(string cadenaConexion) : WebApplicationFactory<Program>
    {
        private readonly Dictionary<string, string?> _entorno = new()
        {
            ["DB_CONNECTION_STRING"] = cadenaConexion,
            ["JWT_SIGNING_KEY"] = "0123456789abcdef0123456789abcdef",
            ["CORS_ALLOWED_ORIGINS"] = "http://localhost:5173",
            ["APPLY_MIGRATIONS"] = "true",
            ["ADMIN_USERNAME"] = "admin",
            ["ADMIN_PASSWORD"] = "Contrasena-de-prueba-1",
        };

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Program lee el entorno del proceso; se fija sólo mientras se construye el host.
            var anteriores = _entorno.ToDictionary(p => p.Key, p => Environment.GetEnvironmentVariable(p.Key));
            try
            {
                foreach (var (clave, valor) in _entorno)
                {
                    Environment.SetEnvironmentVariable(clave, valor);
                }

                return base.CreateHost(builder);
            }
            finally
            {
                foreach (var (clave, valor) in anteriores)
                {
                    Environment.SetEnvironmentVariable(clave, valor);
                }
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseEnvironment("Production");
    }

    [Fact(DisplayName = "001/RF-23: /health responde 200 contra PostgreSQL real")]
    public async Task Health_responde_200_contra_postgres_real()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var api = new ApiReal(cadena);
        using var cliente = api.CreateClient();

        var respuesta = await cliente.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Healthy", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "001/RF-26: la API arranca con migraciones aplicadas")]
    public async Task Api_arranca_con_migraciones_aplicadas()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var api = new ApiReal(cadena);
        using var cliente = api.CreateClient(); // fuerza el arranque real (migraciones + administrador)

        await using var contexto = PostgresFixture.CrearContexto(cadena);
        var aplicadas = await contexto.Database.GetAppliedMigrationsAsync();
        var pendientes = await contexto.Database.GetPendingMigrationsAsync();

        Assert.NotEmpty(aplicadas);
        Assert.Empty(pendientes);
        Assert.True(
            await contexto.Set<Usuario>().AnyAsync(),
            "el administrador inicial debe existir tras el arranque");
    }
}
