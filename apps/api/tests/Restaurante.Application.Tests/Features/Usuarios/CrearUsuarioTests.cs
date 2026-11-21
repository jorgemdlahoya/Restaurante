using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Usuarios;
using Restaurante.Application.Features.Usuarios.CrearUsuario;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Usuarios;

public class CrearUsuarioTests
{
    private readonly UsuarioRepositoryEnMemoria _repositorio = new();
    private readonly HasherFalso _hasher = new();
    private readonly RelojFijo _reloj = new();

    private CrearUsuarioHandler CrearHandler() => new(_repositorio, _hasher, _reloj);

    [Fact(DisplayName = "001/RF-11: crea usuario activo con hash y devuelve DTO sin contraseña")]
    public async Task CreaUsuarioActivoConHash()
    {
        var dto = await CrearHandler().Handle(new CrearUsuarioCommand("Maria", "secreta123", "mesero"), CancellationToken.None);

        var guardado = Assert.Single(_repositorio.Usuarios);
        Assert.Equal("hash:secreta123", guardado.HashContrasena);
        Assert.True(guardado.Activo);
        Assert.Equal(Rol.Mesero, guardado.Rol);
        Assert.Equal(_reloj.Ahora, guardado.CreadoEn);
        Assert.Equal(1, _repositorio.GuardadosLlamados);
        Assert.Equal(new UsuarioDto(guardado.Id, "Maria", "mesero", true), dto);
    }

    [Fact(DisplayName = "001/RF-12: nombre duplicado sin distinguir mayúsculas lanza ConflictoException")]
    public async Task NombreDuplicadoLanzaConflicto()
    {
        _repositorio.Agregar(Usuario.Crear("Maria", "hash:x", Rol.Caja, _reloj.Ahora));

        await Assert.ThrowsAsync<ConflictoException>(() =>
            CrearHandler().Handle(new CrearUsuarioCommand("MARIA", "secreta123", "cocina"), CancellationToken.None));

        Assert.Single(_repositorio.Usuarios);
        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Theory(DisplayName = "001/RF-13: valida formato de nombre, longitud de contraseña y rol")]
    [InlineData("ab", "secreta123", "mesero")]
    [InlineData("con espacio", "secreta123", "mesero")]
    [InlineData("maria", "1234567", "mesero")]
    [InlineData("maria", "secreta123", "Caja")]
    [InlineData("maria", "secreta123", "chef")]
    public void RechazaDatosInvalidos(string nombre, string contrasena, string rol)
    {
        var resultado = new CrearUsuarioValidator().Validate(new CrearUsuarioCommand(nombre, contrasena, rol));

        Assert.False(resultado.IsValid);
    }

    [Theory(DisplayName = "001/RF-13: acepta datos válidos")]
    [InlineData("maria.lopez-1_x", "12345678", "mesero")]
    [InlineData("abc", "secreta123", "cocina")]
    [InlineData("abc", "secreta123", "caja")]
    [InlineData("abc", "secreta123", "administrador")]
    public void AceptaDatosValidos(string nombre, string contrasena, string rol)
    {
        var resultado = new CrearUsuarioValidator().Validate(new CrearUsuarioCommand(nombre, contrasena, rol));

        Assert.True(resultado.IsValid);
    }
}
