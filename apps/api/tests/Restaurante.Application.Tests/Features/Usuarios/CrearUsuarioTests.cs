using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Usuarios;
using Restaurante.Application.Features.Usuarios.CrearUsuario;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Usuarios;

public class CrearUsuarioTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private readonly ContextoRestauranteFalso _contexto = new(RestauranteId);
    private readonly UsuarioRepositoryEnMemoria _repositorio;
    private readonly HasherFalso _hasher = new();
    private readonly RelojFijo _reloj = new();

    public CrearUsuarioTests() => _repositorio = new(_contexto);

    private CrearUsuarioHandler CrearHandler() => new(_repositorio, _hasher, _reloj, _contexto);

    [Fact(DisplayName = "001/RF-11: crea usuario activo con hash y devuelve DTO sin contraseña")]
    public async Task CreaUsuarioActivoConHash()
    {
        var dto = await CrearHandler().Handle(new CrearUsuarioCommand("Maria", "secreta123", "cajero"), CancellationToken.None);

        var guardado = Assert.Single(_repositorio.Usuarios);
        Assert.Equal("hash:secreta123", guardado.HashContrasena);
        Assert.True(guardado.Activo);
        Assert.Equal(Rol.Cajero, guardado.Rol);
        Assert.Equal(_reloj.Ahora, guardado.CreadoEn);
        Assert.Equal(1, _repositorio.GuardadosLlamados);
        Assert.Equal(new UsuarioDto(guardado.Id, "Maria", "cajero", true), dto);
    }

    [Fact(DisplayName = "001/RF-12: nombre duplicado sin distinguir mayúsculas lanza ConflictoException")]
    public async Task NombreDuplicadoLanzaConflicto()
    {
        _repositorio.Agregar(Usuario.Crear(RestauranteId, "Maria", "hash:x", Rol.Cajero, [], _reloj.Ahora));

        await Assert.ThrowsAsync<ConflictoException>(() =>
            CrearHandler().Handle(new CrearUsuarioCommand("MARIA", "secreta123", "cocinero"), CancellationToken.None));

        Assert.Single(_repositorio.Usuarios);
        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Theory(DisplayName = "001/RF-13: valida formato de nombre, longitud de contraseña y rol")]
    [InlineData("ab", "secreta123", "cajero")]
    [InlineData("con espacio", "secreta123", "cajero")]
    [InlineData("maria", "1234567", "cajero")]
    [InlineData("maria", "secreta123", "Cajero")]
    [InlineData("maria", "secreta123", "chef")]
    public void RechazaDatosInvalidos(string nombre, string contrasena, string rol)
    {
        var resultado = new CrearUsuarioValidator().Validate(new CrearUsuarioCommand(nombre, contrasena, rol));

        Assert.False(resultado.IsValid);
    }

    [Theory(DisplayName = "001/RF-13: acepta datos válidos")]
    [InlineData("maria.lopez-1_x", "12345678", "cajero")]
    [InlineData("abc", "secreta123", "cocinero")]
    [InlineData("abc", "secreta123", "cajero")]
    [InlineData("abc", "secreta123", "administrador_general")]
    public void AceptaDatosValidos(string nombre, string contrasena, string rol)
    {
        var resultado = new CrearUsuarioValidator().Validate(new CrearUsuarioCommand(nombre, contrasena, rol));

        Assert.True(resultado.IsValid);
    }
}
