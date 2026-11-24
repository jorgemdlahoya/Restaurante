using System.Text.RegularExpressions;
using FluentValidation;
using Restaurante.Domain.Comun;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales;

/// <summary>Reglas de formato de los datos de una sucursal (RF-02), compartidas por el alta y la edición.</summary>
public sealed partial class ValidadorDatosSucursal : AbstractValidator<DatosSucursalDto>
{
    public ValidadorDatosSucursal()
    {
        RuleFor(x => x.Nombre)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre es obligatorio")
            .Must(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length <= Sucursal.LongitudMaximaNombre)
            .WithMessage("El nombre no puede superar 80 caracteres");

        RuleFor(x => x.Direccion)
            .Must(d => !string.IsNullOrWhiteSpace(d)).WithMessage("La dirección es obligatoria")
            .Must(d => string.IsNullOrWhiteSpace(d) || d.Trim().Length <= Sucursal.LongitudMaximaDireccion)
            .WithMessage("La dirección no puede superar 200 caracteres");

        RuleFor(x => x.Telefono)
            .Must(t => TelefonoMexico.Normalizar(t) is not null)
            .WithMessage("El teléfono debe ser un número mexicano: +52 y 10 dígitos");

        RuleFor(x => x.WhatsApp)
            .Must(t => TelefonoMexico.Normalizar(t) is not null)
            .WithMessage("El WhatsApp debe ser un número mexicano: +52 y 10 dígitos");

        RuleFor(x => x.PhoneNumberId)
            .Must(p => PatronPhoneNumberId().IsMatch(p!))
            .When(x => x.PhoneNumberId is not null)
            .WithMessage("El phone_number_id debe tener sólo dígitos (hasta 30)");

        RuleFor(x => x.Prefijo)
            .Must(p => p is not null && PatronPrefijo().IsMatch(p))
            .WithMessage("El prefijo debe tener de 1 a 3 letras mayúsculas");

        RuleFor(x => x.ZonaHoraria)
            .Must(z => ZonasHorarias.EsAdmitida(z!))
            .When(x => x.ZonaHoraria is not null)
            .WithMessage("La zona horaria no es válida");
    }

    [GeneratedRegex(@"^[0-9]{1,30}\z")]
    private static partial Regex PatronPhoneNumberId();

    [GeneratedRegex(@"^[A-Z]{1,3}\z")]
    private static partial Regex PatronPrefijo();
}
