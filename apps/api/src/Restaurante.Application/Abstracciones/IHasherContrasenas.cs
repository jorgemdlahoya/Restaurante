namespace Restaurante.Application.Abstracciones;

public interface IHasherContrasenas
{
    string Hashear(string contrasena);

    bool Verificar(string hash, string contrasena);
}
