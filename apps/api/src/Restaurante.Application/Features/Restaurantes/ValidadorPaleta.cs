using FluentValidation;
using Restaurante.Domain.Restaurantes;

namespace Restaurante.Application.Features.Restaurantes;

public sealed class ValidadorPaleta : AbstractValidator<PaletaDto>
{
    public const string MensajeColor = "El color debe tener el formato #RRGGBB";

    public ValidadorPaleta()
    {
        RuleFor(x => x.Principal).Must(EsNuloOColor).WithMessage(MensajeColor);
        RuleFor(x => x.Acento).Must(EsNuloOColor).WithMessage(MensajeColor);
        RuleFor(x => x.Oscuro).Must(EsNuloOColor).WithMessage(MensajeColor);
        RuleFor(x => x.Fondo).Must(EsNuloOColor).WithMessage(MensajeColor);
        RuleFor(x => x.Texto).Must(EsNuloOColor).WithMessage(MensajeColor);
    }

    private static bool EsNuloOColor(string? color) => color is null || Paleta.EsColorHex(color);
}
