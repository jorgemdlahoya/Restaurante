using Microsoft.AspNetCore.Identity;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Infrastructure.Seguridad;

public sealed class HasherContrasenas : IHasherContrasenas
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Hashear(string contrasena) => _hasher.HashPassword(null!, contrasena);

    public bool Verificar(string hash, string contrasena) =>
        _hasher.VerifyHashedPassword(null!, hash, contrasena) != PasswordVerificationResult.Failed;
}
