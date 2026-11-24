using FluentValidation;
using Restaurante.Application.Comun;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios.CrearUsuario;

public sealed class CrearUsuarioValidator : AbstractValidator<CrearUsuarioCommand>
{
    internal static readonly string[] RolesValidos = ["administrador_general", "administrador_sucursal", "cajero", "cocinero"];

    /// <summary>Formato del nombre de usuario (RF-13); compartido con el administrador inicial (RF-10).</summary>
    public const string PatronNombreUsuario = "^[A-Za-z0-9._-]{3,50}$";

    /// <summary>Longitud mínima de la contraseña (RF-13); compartida con el administrador inicial (RF-10).</summary>
    public const int LongitudMinimaContrasena = 8;

    public CrearUsuarioValidator()
    {
        RuleFor(x => x.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .Matches(PatronNombreUsuario)
            .WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres: letras, números, punto, guion y guion bajo");

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(LongitudMinimaContrasena).WithMessage("La contraseña debe tener al menos 8 caracteres");

        RuleFor(x => x.Rol)
            .Must(rol => rol is not null && RolesValidos.Contains(rol, StringComparer.Ordinal))
            .WithMessage("El rol debe ser administrador_general, administrador_sucursal, cajero o cocinero");

        // Sucursales por slug (004/RF-02): ninguna vacía, sin repetir (sin distinguir mayúsculas) y con la
        // cardinalidad del rol; la cardinalidad sólo se comprueba con un rol válido.
        RuleFor(x => x.Sucursales)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Las sucursales son obligatorias")
            .Must(s => s.All(slug => !string.IsNullOrWhiteSpace(slug)))
            .WithMessage("Cada sucursal se indica por su slug")
            .Must(s => s.Select(slug => slug.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == s.Count)
            .WithMessage("Las sucursales no pueden repetirse")
            .Must((x, s) => !RolExtensiones.TryDesdeTexto(x.Rol, out var rol) || Usuario.CardinalidadValida(rol, s.Count))
            .WithMessage(x => MensajeCardinalidad(x.Rol));
    }

    private static string MensajeCardinalidad(string rol) => rol switch
    {
        "administrador_general" => "El administrador general no lleva sucursales",
        "administrador_sucursal" => "El administrador de sucursal debe tener exactamente una sucursal",
        _ => "El cajero y el cocinero deben tener al menos una sucursal",
    };
}
