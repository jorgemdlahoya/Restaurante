using Restaurante.Infrastructure.Seguridad;

namespace Restaurante.Infrastructure.Tests.Seguridad;

public sealed class HasherContrasenasTests
{
    [Fact(DisplayName = "001/RF-08: el hash verifica la contraseña y no la contiene")]
    public void Hash_verifica_y_no_contiene_la_contrasena()
    {
        var hasher = new HasherContrasenas();

        var hash = hasher.Hashear("Secreta-123");

        Assert.DoesNotContain("Secreta-123", hash);
        Assert.True(hasher.Verificar(hash, "Secreta-123"));
        Assert.False(hasher.Verificar(hash, "otra"));
    }
}
