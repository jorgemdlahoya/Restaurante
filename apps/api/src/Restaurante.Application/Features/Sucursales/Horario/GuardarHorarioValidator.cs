using FluentValidation;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales.Horario;

public sealed class GuardarHorarioValidator : AbstractValidator<GuardarHorarioCommand>
{
    private const string FormatoInvalido = "La hora debe tener el formato HH:mm";
    private const string HoraFaltante = "Un día abierto necesita hora de apertura y de cierre";
    private const string HorasIguales = "La hora de apertura y la de cierre no pueden ser iguales";

    public GuardarHorarioValidator()
    {
        RuleFor(x => x.Dias)
            .Must(SonLosSieteDias).WithMessage("El horario debe incluir los siete días");

        // Con la semana incompleta o con elementos nulos sólo se informa de `dias`.
        When(x => x.Dias is not null && x.Dias.All(d => d is not null), () =>
        {
            RuleForEach(x => x.Dias).ChildRules(dia =>
            {
                dia.RuleFor(d => d.Apertura)
                    .Must(h => HorarioDiaDto.TryLeerHora(h, out _)).WithMessage(FormatoInvalido)
                    .When(d => !d.Cerrado && !string.IsNullOrEmpty(d.Apertura));
                dia.RuleFor(d => d.Apertura)
                    .Must(h => !string.IsNullOrEmpty(h)).WithMessage(HoraFaltante)
                    .When(d => !d.Cerrado);

                dia.RuleFor(d => d.Cierre)
                    .Must(h => HorarioDiaDto.TryLeerHora(h, out _)).WithMessage(FormatoInvalido)
                    .When(d => !d.Cerrado && !string.IsNullOrEmpty(d.Cierre));
                dia.RuleFor(d => d.Cierre)
                    .Must(h => !string.IsNullOrEmpty(h)).WithMessage(HoraFaltante)
                    .When(d => !d.Cerrado);

                dia.RuleFor(d => d.Cierre)
                    .Must((d, _) => false).WithMessage(HorasIguales)
                    .When(d => !d.Cerrado
                        && HorarioDiaDto.TryLeerHora(d.Apertura, out var apertura)
                        && HorarioDiaDto.TryLeerHora(d.Cierre, out var cierre)
                        && apertura == cierre);
            });
        });
    }

    private static bool SonLosSieteDias(IReadOnlyList<HorarioDiaDto>? dias) =>
        dias is { Count: 7 }
        && dias.All(d => d is not null && DiasSemana.TryDesdeTexto(d.Dia, out _))
        && dias.Select(d => d.Dia).Distinct().Count() == 7;
}
