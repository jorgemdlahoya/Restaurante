using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Usuarios;
using Restaurante.Application.Features.Usuarios.CambiarRolUsuario;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Usuarios;

public class CambiarRolUsuarioTests
{
    private readonly UsuarioRepositoryEnMemoria _repositorio = new();
    private readonly RelojFijo _reloj = new();

    private Usuario Agregar(string nombre, Rol rol)
    {
        var u = Usuario.Crear(nombre, "hash:x", rol, _reloj.Ahora);
        _repositorio.Agregar(u);
        return u;
    }

    private Task<UsuarioDto> Ejecutar(Guid id, string rol) =>
        new CambiarRolUsuarioHandler(_repositorio)
            .Handle(new CambiarRolUsuarioCommand(id, rol), CancellationToken.None);

    [Fact(DisplayName = "001/RF-16: cambia el rol")]
    public async Task CambiaElRol()
    {
        var u = Agregar("pepe", Rol.Cajero);

        var dto = await Ejecutar(u.Id, "cajero");

        Assert.Equal("cajero", dto.Rol);
        Assert.Equal(Rol.Cajero, u.Rol);
        Assert.Equal(1, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "001/RF-16: valida que el rol sea uno del conjunto exacto")]
    public void ValidaRol()
    {
        var validador = new CambiarRolUsuarioValidator();

        Assert.True(validador.Validate(new CambiarRolUsuarioCommand(Guid.NewGuid(), "cocinero")).IsValid);
        Assert.False(validador.Validate(new CambiarRolUsuarioCommand(Guid.NewGuid(), "Cajero")).IsValid);
        Assert.False(validador.Validate(new CambiarRolUsuarioCommand(Guid.NewGuid(), "chef")).IsValid);
    }

    [Fact(DisplayName = "001/RF-17: no quita el rol al último administrador activo")]
    public async Task NoQuitaRolAlUltimoAdministrador()
    {
        Agregar("viejo", Rol.AdministradorGeneral).Desactivar();
        var unico = Agregar("jefe", Rol.AdministradorGeneral);

        await Assert.ThrowsAsync<ConflictoException>(() => Ejecutar(unico.Id, "cajero"));

        Assert.Equal(Rol.AdministradorGeneral, unico.Rol);
        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "001/RF-17: mantener administrador al último administrador no es conflicto")]
    public async Task MismoRolNoEsConflicto()
    {
        var unico = Agregar("jefe", Rol.AdministradorGeneral);

        var dto = await Ejecutar(unico.Id, "administrador_general");

        Assert.Equal("administrador_general", dto.Rol);
    }

    [Fact(DisplayName = "001/RF-17: quita el rol de administrador si queda otro activo")]
    public async Task QuitaRolSiQuedaOtro()
    {
        Agregar("admin", Rol.AdministradorGeneral);
        var otro = Agregar("jefe", Rol.AdministradorGeneral);

        var dto = await Ejecutar(otro.Id, "cocinero");

        Assert.Equal("cocinero", dto.Rol);
    }

    [Fact(DisplayName = "001/RF-17: cambiar el rol a un administrador inactivo no cuenta como último activo")]
    public async Task AdministradorInactivoSePuedeCambiar()
    {
        Agregar("admin", Rol.AdministradorGeneral);
        var inactivo = Agregar("viejo", Rol.AdministradorGeneral);
        inactivo.Desactivar();

        var dto = await Ejecutar(inactivo.Id, "cajero");

        Assert.Equal("cajero", dto.Rol);
    }

    [Fact(DisplayName = "001/RF-19: id inexistente lanza NoEncontradoException")]
    public async Task IdInexistenteLanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() => Ejecutar(Guid.NewGuid(), "cajero"));
    }
}
