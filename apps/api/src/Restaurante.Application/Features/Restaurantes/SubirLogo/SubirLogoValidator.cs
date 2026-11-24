using FluentValidation;

namespace Restaurante.Application.Features.Restaurantes.SubirLogo;

public sealed class SubirLogoValidator : AbstractValidator<SubirLogoCommand>
{
    public const int TamanoMaximo = 1_048_576;

    public SubirLogoValidator()
    {
        RuleFor(x => x.Logo)
            .Cascade(CascadeMode.Stop)
            .Must(l => l is { Length: > 0 }).WithMessage("El logo es obligatorio")
            .Must(l => l.Length <= TamanoMaximo).WithMessage("El logo no puede superar 1 MB")
            .Must(l => DetectorTipoImagen.Detectar(l) is not null).WithMessage("El logo debe ser PNG, JPG o WebP");
    }
}
