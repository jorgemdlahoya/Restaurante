using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Tests.Dobles;

/// <summary>Hasher determinista: el "hash" es "hash:" + contraseña.</summary>
public sealed class HasherFalso : IHasherContrasenas
{
    public string Hashear(string contrasena) => "hash:" + contrasena;

    public bool Verificar(string hash, string contrasena) => hash == "hash:" + contrasena;
}
