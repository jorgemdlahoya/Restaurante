using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Restaurante.Application.Tests.Comun;

public sealed record PruebaCommand(string Nombre, string Contrasena) : IRequest<string>;

public sealed class PruebaCommandValidator : AbstractValidator<PruebaCommand>
{
    public PruebaCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es obligatorio");
        RuleFor(x => x.Contrasena).MinimumLength(8).WithMessage("La contraseña es demasiado corta");
    }
}

public sealed class LoggerGrabador<T> : ILogger<T>
{
    public List<(LogLevel Nivel, string Mensaje, Exception? Excepcion)> Entradas { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Entradas.Add((logLevel, formatter(state, exception), exception));
}
