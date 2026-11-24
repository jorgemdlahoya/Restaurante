using System.Collections;
using Restaurante.Application;
using Restaurante.Infrastructure;
using Restaurante.WebApi;
using Restaurante.WebApi.Arranque;
using Restaurante.WebApi.Configuracion;
using Restaurante.WebApi.Seguridad;

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
    .AddWebApiServices(configuracion.ClaveJwt, configuracion.OrigenesCors);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerConBearer();
}

var app = builder.Build();

// Primeros del pipeline: convierten en problem+json las excepciones de middleware y las respuestas
// de error sin cuerpo (404 de ruta, 405, 415). Las respuestas de MVC conservan su forma.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurante API v1"));
}

app.UseCors(Restaurante.WebApi.DependencyInjection.PoliticaCors);
app.UseAuthentication();
// Tras la autenticación: decide el restaurante de la petición (slug de la ruta o token) antes de los controllers.
app.UseMiddleware<MiddlewareContextoRestaurante>();
// Tras el restaurante: en /api/r/{slug}/s/{sucursal}… fija la sucursal (dentro de ese restaurante) o responde 404.
app.UseMiddleware<MiddlewareContextoSucursal>();
app.MapControllers();
app.MapHealthChecks("/health"); // anónimo (RF-07): no es una acción de controller.

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
