using FluentValidation;
using MediatR;

namespace Restaurante.Application.Comun.Behaviors;

/// <summary>Ejecuta todos los validators del request y lanza ValidationException con todos los errores.</summary>
public sealed class ValidacionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidacionBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var contexto = new ValidationContext<TRequest>(request);
            var resultados = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(contexto, cancellationToken)));
            var errores = resultados.SelectMany(r => r.Errors).Where(e => e is not null).ToList();

            if (errores.Count > 0)
                throw new ValidationException(errores);
        }

        return await next(cancellationToken);
    }
}
