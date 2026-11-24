using FluentValidation;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales.Cierres;

public sealed class CrearCierreValidator : AbstractValidator<CrearCierreCommand>
{
    public const string FechaInvalida = "La fecha debe tener el formato yyyy-MM-ddTHH:mm";
    public const string MotivoLargo = "El motivo no puede superar 120 caracteres";

    public CrearCierreValidator()
    {
        RuleFor(x => x.Inicio)
            .Must(f => HoraLocal.TryLeer(f, out _)).WithMessage(FechaInvalida)
            .When(x => x.Inicio is not null);

        RuleFor(x => x.Fin)
            .Must(f => HoraLocal.TryLeer(f, out _)).WithMessage(FechaInvalida);

        RuleFor(x => x.Motivo)
            .Must(m => m!.Trim().Length <= CierreTemporal.LongitudMaximaMotivo).WithMessage(MotivoLargo)
            .When(x => x.Motivo is not null);
    }
}
