using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun;
using Restaurante.Domain.Usuarios;

namespace Restaurante.WebApi.Tests.Infraestructura;

/// <summary>
/// Colección que serializa las clases que usan la fábrica: el entorno del proceso es global
/// y <see cref="FabricaApi"/> lo modifica mientras construye el host.
/// </summary>
[CollectionDefinition(Nombre, DisableParallelization = true)]
public sealed class ColeccionApi
{
    public const string Nombre = "Api";
}

internal sealed class SaludBaseDePrueba(Func<bool> accesible) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(accesible() ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("base inaccesible"));
}

public sealed class FabricaApi :WebApplicationFactory<Program>
{
    public const string ClaveJwt = "0123456789abcdef0123456789abcdef";
    public const string ClaveAjena = "ffffffffffffffffffffffffffffffff";

    private readonly Dictionary<string, string?> _entorno;

    private readonly string _entornoHospedaje;

    public FabricaApi(
        Action<Dictionary<string, string?>>? ajustarEntorno = null,
        string entornoHospedaje = "Development")
    {
        var ajustado = EntornoValido();
        ajustarEntorno?.Invoke(ajustado);

        // Toda variable conocida que el ajuste quite queda en null de forma explícita: así se
        // borra del proceso en lugar de heredar lo que el desarrollador tenga exportado.
        _entorno = EntornoValido().Keys.ToDictionary(k => k, k => (string?)null);
        foreach (var (clave, valor) in ajustado)
        {
            _entorno[clave] = valor;
        }

        _entornoHospedaje = entornoHospedaje;
    }

    /// <summary>Resultado que devuelve el health check de base de datos de prueba.</summary>
    public bool BaseAccesible { get; set; } = true;

    public UsuarioRepositoryEnMemoria Usuarios { get; } = new();

    public IReadOnlyDictionary<string, string?> Entorno => _entorno;

    /// <summary>Ajuste opcional de servicios que cada prueba aplica sobre el host (p. ej. provocar un fallo en un middleware).</summary>
    public Action<IServiceCollection>? AjustarServicios { get; init; }

    /// <summary>Salida de consola (logs de arranque) emitida mientras se construía el host.</summary>
    public string SalidaDeArranque { get; private set; } = string.Empty;

    public static Dictionary<string, string?> EntornoValido() => new()
    {
        ["DB_CONNECTION_STRING"] = "Host=no-existe;Database=prueba;Username=u;Password=p",
        ["JWT_SIGNING_KEY"] = ClaveJwt,
        ["CORS_ALLOWED_ORIGINS"] = "http://localhost:5173",
        ["APPLY_MIGRATIONS"] = "false",
        ["ADMIN_USERNAME"] = "admin",
        ["ADMIN_PASSWORD"] = "Contrasena-de-prueba-1",
    };

    public static string EmitirToken(
        Rol rol, string clave = ClaveJwt, TimeSpan? expiraEn = null, Guid? usuarioId = null,
        string emisor = "restaurante-api", string audiencia = "restaurante-web")
    {
        var ahora = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: emisor,
            audience: audiencia,
            claims:
            [
                new Claim("sub", (usuarioId ?? Guid.NewGuid()).ToString()),
                new Claim("unique_name", "usuario-prueba"),
                new Claim("role", rol.ComoTexto()),
            ],
            notBefore: ahora.AddHours(-3),
            expires: ahora.Add(expiraEn ?? TimeSpan.FromHours(1)),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Program lee el entorno del proceso; se sustituye sólo mientras se construye el host.
        var anteriores = _entorno.ToDictionary(p => p.Key, p => Environment.GetEnvironmentVariable(p.Key));
        var consolaOriginal = Console.Out;
        var captura = new StringWriter();
        Console.SetOut(captura);
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
            Console.SetOut(consolaOriginal);
            SalidaDeArranque = captura.ToString();
            foreach (var (clave, valor) in anteriores)
            {
                Environment.SetEnvironmentVariable(clave, valor);
            }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(servicios =>
            servicios.AddControllers().AddApplicationPart(typeof(FabricaApi).Assembly));

        builder.UseEnvironment(_entornoHospedaje);

        builder.ConfigureServices(servicios =>
        {
            AjustarServicios?.Invoke(servicios);
            servicios.RemoveAll<IUsuarioRepository>();
            servicios.AddSingleton<IUsuarioRepository>(Usuarios);

            // Sustituye el health check real de EF Core (que necesita PostgreSQL) por uno controlable.
            servicios.Configure<HealthCheckServiceOptions>(o =>
            {
                o.Registrations.Clear();
                o.Registrations.Add(new HealthCheckRegistration(
                    "base-de-datos",
                    _ => new SaludBaseDePrueba(() => BaseAccesible),
                    failureStatus: null,
                    tags: null));
            });
        });
    }
}
