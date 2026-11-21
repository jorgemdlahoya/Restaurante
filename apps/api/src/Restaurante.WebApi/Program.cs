using System.Collections;
using Restaurante.Application;
using Restaurante.Infrastructure;
using Restaurante.WebApi;
using Restaurante.WebApi.Arranque;
using Restaurante.WebApi.Configuracion;

// 1. Configuración desde el entorno; si es inválida no se construye la API.
var entorno = Environment.GetEnvironmentVariables()
    .Cast<DictionaryEntry>()
    .ToDictionary(e => (string)e.Key, e => (string?)e.Value);

var configuracion = ConfiguracionEntorno.Cargar(entorno);
if (!configuracion.EsValida)
{
    using var fabricaLogs = LoggerFactory.Create(b => b.AddSimpleConsole());
    var logArranque = fabricaLogs.CreateLogger("Arranque");
    foreach (var error in configuracion.Errores)
    {
        logArranque.LogCritical("Configuración inválida: {Error}", error);
    }

    return 1;
}

// 2. Construcción del host.
var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(configuracion.CadenaConexion, configuracion.ClaveJwt)
    .AddWebApiServices(configuracion.ClaveJwt);

var app = builder.Build();
app.UseAuthentication();
app.MapControllers();

// 3. Migraciones (opcionales) y administrador inicial.
var arranqueOk = await SecuenciaArranque.EjecutarAsync(
    app.Services,
    entorno,
    configuracion.AplicarMigraciones,
    app.Logger,
    app.Lifetime.ApplicationStopping);
if (!arranqueOk)
{
    return 1;
}

// 4. Servir peticiones.
await app.RunAsync();
return 0;

/// <summary>Visible para <c>WebApplicationFactory&lt;Program&gt;</c> en las pruebas.</summary>
public partial class Program;
