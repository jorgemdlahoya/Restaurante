using FluentValidation;

namespace Restaurante.Application.Features.Usuarios.EditarUsuario;

/// <summary>Mismas reglas de rol y sucursales que el alta (004/RF-04 → RF-02).</summary>
public sealed class EditarUsuarioValidator : AbstractValidator<EditarUsuarioCommand>
{
    public EditarUsuarioValidator() =>
        ReglasRolYSucursales.Aplicar(this, x => x.Rol, x => x.Sucursales);
}
