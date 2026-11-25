using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Restaurante.WebApi.Filtros;

/// <summary>Construye los 400 de validación con una única forma, vengan del validador o del enlace del modelo.</summary>
public static class RespuestasProblema
{
    public const string MensajeCuerpoInvalido = "El cuerpo de la petición no es válido.";

    public static ObjectResult Validacion(IEnumerable<KeyValuePair<string, string[]>> errores)
    {
        var detalles = new ValidationProblemDetails(
            errores
                .GroupBy(e => ACamelCase(e.Key))
                .ToDictionary(g => g.Key, g => g.SelectMany(e => e.Value).ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Datos inválidos",
            Detail = "Uno o más campos no son válidos.",
        };
        return Resultado(detalles);
    }

    /// <summary>
    /// Fallos de enlace del modelo (JSON mal formado, tipos incorrectos, cuerpo ausente). Los campos
    /// faltantes o vacíos los resuelven los validadores, así que aquí todo es un cuerpo inválido.
    /// </summary>
    public static ObjectResult Validacion(ModelStateDictionary modelState) =>
        Validacion(modelState, new HashSet<string>());

    /// <summary>
    /// Igual que la sobrecarga sin parámetros de consulta, pero un fallo de enlace de un parámetro de
    /// consulta (<paramref name="parametrosConsulta"/>, p. ej. <c>pagina</c>) conserva su nombre como clave.
    /// </summary>
    public static ObjectResult Validacion(ModelStateDictionary modelState, IReadOnlySet<string> parametrosConsulta) =>
        Validacion(modelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .Select(e => KeyValuePair.Create(
                ClaveDeEnlace(e.Key, parametrosConsulta),
                new[] { MensajeCuerpoInvalido })));

    private static string ClaveDeEnlace(string clave, IReadOnlySet<string> parametrosConsulta)
    {
        if (clave.StartsWith("$.", StringComparison.Ordinal))
        {
            return clave[2..];
        }

        return parametrosConsulta.Contains(clave) ? clave : "cuerpo";
    }

    /// <summary>
    /// Títulos en español de los errores que no genera MVC (ruteo, negociación de contenido, middleware).
    /// Los demás estados conservan el título por defecto.
    /// </summary>
    public static string? TituloFueraDeMvc(int? estado) => estado switch
    {
        StatusCodes.Status404NotFound => "Recurso no encontrado",
        StatusCodes.Status405MethodNotAllowed => "Método no permitido",
        StatusCodes.Status415UnsupportedMediaType => "Tipo de contenido no admitido",
        StatusCodes.Status500InternalServerError => "Error interno",
        _ => null,
    };

    /// <summary>
    /// Adapta un <see cref="ProblemDetails"/> generado por el framework (status code pages, exception
    /// handler, factoría de MVC): títulos en español y, en 500, traceId sin ningún detalle interno.
    /// </summary>
    public static void Personalizar(ProblemDetailsContext contexto)
    {
        var detalles = contexto.ProblemDetails;
        if (TituloFueraDeMvc(detalles.Status) is { } titulo)
        {
            detalles.Title = titulo;
        }

        if (detalles.Status == StatusCodes.Status500InternalServerError)
        {
            detalles.Detail = null;
            detalles.Extensions["traceId"] = Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier;
        }
    }

    public static ObjectResult Resultado(ProblemDetails detalles) =>
        new(detalles) { StatusCode = detalles.Status, ContentTypes = { "application/problem+json" } };

    /// <summary>Escribe un problem+json directamente en la respuesta, para middlewares que cortan la petición fuera de MVC.</summary>
    public static Task EscribirAsync(HttpContext contexto, int estado, string titulo, string detalle)
    {
        contexto.Response.StatusCode = estado;
        return contexto.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = estado, Title = titulo, Detail = detalle },
            options: null,
            contentType: "application/problem+json",
            cancellationToken: contexto.RequestAborted);
    }

    /// <summary>Errores por campo con las claves en camelCase, para los problemas que no son un 400 de validación.</summary>
    public static Dictionary<string, string[]> ConClavesCamelCase(IReadOnlyDictionary<string, string[]> errores) =>
        errores.ToDictionary(e => ACamelCase(e.Key), e => e.Value);

    private static string ACamelCase(string clave)
    {
        // Claves anidadas ("a.B") y vacías (cuerpo completo) se conservan salvo la inicial de cada segmento.
        return string.Join('.', clave.Split('.').Select(s =>
            s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s[1..]));
    }
}
