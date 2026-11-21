using Microsoft.OpenApi;
using Restaurante.WebApi.Filtros;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Restaurante.WebApi.Seguridad;

/// <summary>
/// Declara el requisito Bearer sólo en las operaciones protegidas con <see cref="RequiereRolAttribute"/>
/// (en la acción o en su controller); login y health no aparecen como si exigieran token.
/// </summary>
public sealed class RequisitoBearerOperationFilter : IOperationFilter
{
    public const string EsquemaBearer = "Bearer";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var protegida = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<RequiereRolAttribute>()
            .Any();
        if (!protegida)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(EsquemaBearer, context.Document)] = [],
        });
    }
}
