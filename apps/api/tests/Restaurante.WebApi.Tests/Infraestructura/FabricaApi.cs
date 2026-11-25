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
using Restaurante.Domain.Restaurantes;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;
using Restaurante.Infrastructure.Persistencia;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

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

    /// <summary>Almacén compartido; cada petición lo ve filtrado por su contexto de restaurante.</summary>
    public AlmacenUsuarios Usuarios { get; } = new();

    public AlmacenSucursales Sucursales { get; } = new();

    public AlmacenCierres Cierres { get; } = new();

    public RestauranteRepositoryEnMemoria Restaurantes { get; } = new();

    /// <summary>Siembra un restaurante (activo o no) en el repositorio en memoria y lo devuelve.</summary>
    public RestauranteEntidad SembrarRestaurante(string nombre, string slug, bool activo = true)
    {
        var restaurante = RestauranteEntidad.Crear(nombre, slug, Paleta.Vacia, DateTimeOffset.UtcNow);
        if (!activo)
        {
            restaurante.Desactivar();
        }

        Restaurantes.Agregar(restaurante);
        return restaurante;
    }

    private int _sucursalesSembradas;

    /// <summary>Siembra una sucursal activa del restaurante en el almacén en memoria y la devuelve.</summary>
    public Sucursal SembrarSucursal(Guid restauranteId, string slug, string nombre, string prefijo = "S")
    {
        var whatsApp = $"+52559{Interlocked.Increment(ref _sucursalesSembradas):D7}";
        var sucursal = Sucursal.Crear(
            restauranteId,
            slug,
            new DatosSucursal(nombre, "Calle 1", "+525500000000", whatsApp, null, prefijo, ZonasHorarias.PorDefecto),
            DateTimeOffset.UtcNow);
        Sucursales.Sucursales.Add(sucursal);
        return sucursal;
    }

    /// <summary>
    /// Sucursales mínimas que exige el rol (004/RF-02): ninguna para los administradores general y de plataforma;
    /// para el resto, una sucursal activa nueva sembrada en el restaurante.
    /// </summary>
    public IReadOnlyCollection<Guid> SucursalesPara(Rol rol, Guid? restauranteId) =>
        Usuario.CardinalidadValida(rol, 0) || restauranteId is not { } id
            ? []
            : [SembrarSucursal(id, $"suc-{Guid.NewGuid():N}"[..20], "Sucursal de prueba").Id];

    public IReadOnlyDictionary<string, string?> Entorno => _entorno;

    /// <summary>Ajuste opcional de servicios que cada prueba aplica sobre el host (p. ej. provocar un fallo en un middleware).</summary>
    public Action<IServiceCollection>? AjustarServicios { get; init; }

    // El registro en consola guarda el escritor que había al crearse (esta captura) pero escribe en segundo
    // plano: un mensaje del arranque puede llegar después de que CreateHost termine. Por eso la captura se lee
    // al consultarla y, para esperar un mensaje concreto, se usa EsperarSalidaDeArranque.
    private StringWriter? _textoCapturado;
    private TextWriter? _captura;

    /// <summary>Salida de consola (logs de arranque) emitida mientras se construía el host, leída en este momento.</summary>
    public string SalidaDeArranque
    {
        get
        {
            if (_captura is null || _textoCapturado is null)
            {
                return string.Empty;
            }

            // TextWriter.Synchronized bloquea sobre el propio escritor: se lee con el mismo cerrojo.
            lock (_captura)
            {
                return _textoCapturado.ToString();
            }
        }
    }

    /// <summary>
    /// Espera (hasta 5 s) a que la salida de arranque contenga <paramref name="texto"/> y la devuelve; si no llega,
    /// devuelve la salida que haya para que la aserción de la prueba muestre lo que se registró.
    /// </summary>
    public string EsperarSalidaDeArranque(string texto)
    {
        var limite = DateTime.UtcNow.AddSeconds(5);
        while (!SalidaDeArranque.Contains(texto, StringComparison.Ordinal) && DateTime.UtcNow < limite)
        {
            Thread.Sleep(20);
        }

        return SalidaDeArranque;
    }

    public static Dictionary<string, string?> EntornoValido() => new()
    {
        ["DB_CONNECTION_STRING"] = "Host=no-existe;Database=prueba;Username=u;Password=p",
        ["JWT_SIGNING_KEY"] = ClaveJwt,
        ["CORS_ALLOWED_ORIGINS"] = "http://localhost:5173",
        ["APPLY_MIGRATIONS"] = "false",
        ["ADMIN_USERNAME"] = "admin",
        ["ADMIN_PASSWORD"] = "Contrasena-de-prueba-1",
    };

    /// <summary>
    /// Firma un token para un usuario del almacén y le pone su sello vigente (004/RF-13), para que lo acepte
    /// <c>MiddlewareSesionUsuario</c>. Con <paramref name="usuarioId"/> usa ese usuario (debe estar sembrado);
    /// sin él siembra uno activo con el rol y el restaurante dados. El token lleva los claims indicados, no los
    /// del usuario: un rol de restaurante sin <paramref name="restauranteId"/> se siembra en un restaurante al
    /// azar y el token sale sin <c>restaurant_id</c>. Cajero, cocinero y administrador de sucursal sembrados así
    /// llevan una sucursal inexistente (004/RF-02).
    /// </summary>
    public string EmitirToken(
        Rol rol, string clave = ClaveJwt, TimeSpan? expiraEn = null, Guid? usuarioId = null,
        string emisor = "restaurante-api", string audiencia = "restaurante-web", Guid? restauranteId = null)
    {
        var usuario = usuarioId is { } id
            ? Usuarios.Usuarios.Single(u => u.Id == id)
            : SembrarUsuarioDeToken(rol, restauranteId);
        return FirmarToken(rol, usuario.Id, usuario.SelloSesion.ToString("D"), clave, expiraEn, emisor, audiencia,
            restauranteId);
    }

    private Usuario SembrarUsuarioDeToken(Rol rol, Guid? restauranteId)
    {
        var restauranteDelUsuario = rol == Rol.AdministradorPlataforma ? (Guid?)null : restauranteId ?? Guid.NewGuid();
        // Cardinalidad de 004/RF-02: los roles que la exigen reciben una sucursal cualquiera (no sembrada). Quien
        // necesite una sucursal real siembra el usuario y pasa su usuarioId.
        IReadOnlyCollection<Guid> sucursales = Usuario.CardinalidadValida(rol, 0) ? [] : [Guid.NewGuid()];
        var usuario = Usuario.Crear(
            restauranteDelUsuario, $"token-{Guid.NewGuid():N}", "sin-hash", rol, sucursales, DateTimeOffset.UtcNow);
        Usuarios.Agregar(usuario);
        return usuario;
    }

    /// <summary>Firma a mano un token con los claims dados; <paramref name="sello"/> nulo lo deja sin el claim.</summary>
    public static string FirmarToken(
        Rol rol, Guid usuarioId, string? sello, string clave = ClaveJwt, TimeSpan? expiraEn = null,
        string emisor = "restaurante-api", string audiencia = "restaurante-web", Guid? restauranteId = null)
    {
        var ahora = DateTime.UtcNow;
        List<Claim> claims =
        [
            new Claim("sub", usuarioId.ToString()),
            new Claim("unique_name", "usuario-prueba"),
            new Claim("role", rol.ComoTexto()),
        ];
        if (restauranteId is { } id)
        {
            claims.Add(new Claim("restaurant_id", id.ToString()));
        }

        if (sello is not null)
        {
            claims.Add(new Claim("sello", sello));
        }

        var token = new JwtSecurityToken(
            issuer: emisor,
            audience: audiencia,
            claims: claims,
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
        // Sincronizado: el hilo del registro en consola puede escribir mientras la prueba lee.
        _textoCapturado = new StringWriter();
        var captura = TextWriter.Synchronized(_textoCapturado);
        _captura = captura;
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
            // Scoped: cada petición filtra el almacén compartido con el contexto que fija el middleware real.
            servicios.AddScoped<IUsuarioRepository>(sp =>
                new UsuarioRepositoryEnMemoria(Usuarios, sp.GetRequiredService<IContextoRestaurante>()));
            servicios.RemoveAll<ISucursalRepository>();
            servicios.AddScoped<ISucursalRepository>(sp =>
                new SucursalRepositoryEnMemoria(
                    Sucursales, sp.GetRequiredService<IContextoRestaurante>(), sp.GetRequiredService<IContextoSucursal>()));
            servicios.RemoveAll<ICierreTemporalRepository>();
            servicios.AddScoped<ICierreTemporalRepository>(sp =>
                new CierreTemporalRepositoryEnMemoria(
                    Cierres, sp.GetRequiredService<IContextoRestaurante>(), sp.GetRequiredService<IContextoSucursal>()));
            servicios.RemoveAll<IRestauranteRepository>();
            servicios.AddSingleton<IRestauranteRepository>(Restaurantes);

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
