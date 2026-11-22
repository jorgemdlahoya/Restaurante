using Restaurante.Application.Comun;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Comun;

public sealed class RolExtensionesTests
{
    [Theory(DisplayName = "002/RF-17: cada rol se expone con su texto de la constitución")]
    [InlineData(Rol.AdministradorPlataforma, "administrador_plataforma")]
    [InlineData(Rol.AdministradorGeneral, "administrador_general")]
    [InlineData(Rol.AdministradorSucursal, "administrador_sucursal")]
    [InlineData(Rol.Cajero, "cajero")]
    [InlineData(Rol.Cocinero, "cocinero")]
    public void Cada_rol_se_expone_con_su_texto(Rol rol, string texto)
    {
        Assert.Equal(texto, rol.ComoTexto());
        Assert.Equal(rol, RolExtensiones.DesdeTexto(texto));
        Assert.True(RolExtensiones.TryDesdeTexto(texto, out var leido));
        Assert.Equal(rol, leido);
    }

    [Theory(DisplayName = "002/RF-17: los textos antiguos o con otra grafía no son roles")]
    [InlineData("administrador")]
    [InlineData("mesero")]
    [InlineData("Cajero")]
    [InlineData("administradorplataforma")]
    [InlineData(null)]
    public void Textos_antiguos_no_son_roles(string? texto)
    {
        Assert.False(RolExtensiones.TryDesdeTexto(texto, out _));
        if (texto is not null)
        {
            Assert.Throws<ArgumentException>(() => RolExtensiones.DesdeTexto(texto));
        }
    }
}
