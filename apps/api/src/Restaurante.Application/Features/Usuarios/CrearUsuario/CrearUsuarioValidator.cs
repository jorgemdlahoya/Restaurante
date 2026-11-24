using FluentValidation;

namespace Restaurante.Application.Features.Usuarios.CrearUsuario;

public sealed class CrearUsuarioValidator : AbstractValidator<CrearUsuarioCommand>
{
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

        // Rol y sucursales por slug con la cardinalidad de 004/RF-02 (compartidas con la edición, RF-04).
        ReglasRolYSucursales.Aplicar(this, x => x.Rol, x => x.Sucursales);
    }
}
