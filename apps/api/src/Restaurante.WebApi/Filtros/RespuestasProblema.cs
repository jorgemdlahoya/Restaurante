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
        Validacion(modelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .Select(e => KeyValuePair.Create(
                e.Key.StartsWith("$.", StringComparison.Ordinal) ? e.Key[2..] : "cuerpo",
                new[] { MensajeCuerpoInvalido })));

    public static ObjectResult Resultado(ProblemDetails detalles) =>
        new(detalles) { StatusCode = detalles.Status, ContentTypes = { "application/problem+json" } };

    private static string ACamelCase(string clave)
    {
        // Claves anidadas ("a.B") y vacías (cuerpo completo) se conservan salvo la inicial de cada segmento.
        return string.Join('.', clave.Split('.').Select(s =>
            s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s[1..]));
    }
}
