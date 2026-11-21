using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Restaurante.Application.Comun.Excepciones;

namespace Restaurante.WebApi.Filtros;

/// <summary>Traduce las excepciones a <see cref="ProblemDetails"/>; nunca expone detalles internos.</summary>
public sealed class FiltroExcepciones(ILogger<FiltroExcepciones> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        context.Result = context.Exception switch
        {
            ValidationException validacion => Validacion(validacion),
            CredencialesInvalidasException e => Problema(StatusCodes.Status401Unauthorized, "No autenticado", e.Message),
            NoEncontradoException e => Problema(StatusCodes.Status404NotFound, "No encontrado", e.Message),
            ConflictoException e => Problema(StatusCodes.Status409Conflict, "Conflicto", e.Message),
            _ => ErrorInterno(context),
        };
        context.ExceptionHandled = true;
    }

    private static ObjectResult Validacion(ValidationException excepcion)
    {
        var errores = excepcion.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var detalles = new ValidationProblemDetails(errores)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Datos inválidos",
            Detail = "Uno o más campos no son válidos.",
        };
        return Resultado(detalles);
    }

    private ObjectResult ErrorInterno(ExceptionContext context)
    {
        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        logger.LogError(context.Exception, "Excepción no controlada. traceId={TraceId}", traceId);

        var detalles = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Error interno",
            Detail = "Ocurrió un error inesperado. Indique el identificador de traza al soporte.",
        };
        detalles.Extensions["traceId"] = traceId;
        return Resultado(detalles);
    }

    private static ObjectResult Problema(int estado, string titulo, string detalle) =>
        Resultado(new ProblemDetails { Status = estado, Title = titulo, Detail = detalle });

    private static ObjectResult Resultado(ProblemDetails detalles) =>
        new(detalles) { StatusCode = detalles.Status, ContentTypes = { "application/problem+json" } };
}
