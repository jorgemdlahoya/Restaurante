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
    private readonly SucursalRepositoryEnMemoria _sucursales;

    public CrearUsuarioTests()
    {
        _repositorio = new(_contexto);
        _sucursales = new(_contexto);
        _sucursales.Sucursales.Add(SucursalesDePrueba.Crear(RestauranteId, "centro", "Centro", "C", "+525511111111"));
    }

    private CrearUsuarioHandler CrearHandler() => new(_repositorio, _hasher, _reloj, _contexto,
        new ResolutorActor(new UsuarioActualFalso(Guid.NewGuid()), _sucursales));

    // Desde 004/RF-02 cajero y cocinero llevan al menos una sucursal.
    private static CrearUsuarioCommand Comando(string nombre, string contrasena, string rol) =>
        new(nombre, contrasena, rol, rol is "cajero" or "cocinero" or "administrador_sucursal" ? ["centro"] : []);

    [Fact(DisplayName = "001/RF-11: crea usuario activo con hash y devuelve DTO sin contraseña")]
    public async Task CreaUsuarioActivoConHash()
    {
        var dto = await CrearHandler().Handle(Comando("Maria", "secreta123", "cajero"), CancellationToken.None);

        var guardado = Assert.Single(_repositorio.Usuarios);
        Assert.Equal("hash:secreta123", guardado.HashContrasena);
        Assert.True(guardado.Activo);
        Assert.Equal(Rol.Cajero, guardado.Rol);
        Assert.Equal(_reloj.Ahora, guardado.CreadoEn);
        Assert.Equal(1, _repositorio.GuardadosLlamados);
        Assert.Equal((guardado.Id, "Maria", "cajero", true), (dto.Id, dto.NombreUsuario, dto.Rol, dto.Activo));
        Assert.Equal([new SucursalAsignadaDto("centro", "Centro", true)], dto.Sucursales);
    }

    [Fact(DisplayName = "001/RF-12: nombre duplicado sin distinguir mayúsculas lanza ConflictoException")]
    public async Task NombreDuplicadoLanzaConflicto()
    {
        _repositorio.Agregar(Usuario.Crear(RestauranteId, "Maria", "hash:x", Rol.Cajero, [Guid.NewGuid()], _reloj.Ahora));

        await Assert.ThrowsAsync<ConflictoException>(() =>
            CrearHandler().Handle(Comando("MARIA", "secreta123", "cocinero"), CancellationToken.None));

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
        var resultado = new CrearUsuarioValidator().Validate(Comando(nombre, contrasena, rol));

        Assert.False(resultado.IsValid);
    }

    [Theory(DisplayName = "001/RF-13: acepta datos válidos")]
    [InlineData("maria.lopez-1_x", "12345678", "cajero")]
    [InlineData("abc", "secreta123", "cocinero")]
    [InlineData("abc", "secreta123", "cajero")]
    [InlineData("abc", "secreta123", "administrador_general")]
    public void AceptaDatosValidos(string nombre, string contrasena, string rol)
    {
        var resultado = new CrearUsuarioValidator().Validate(Comando(nombre, contrasena, rol));

        Assert.True(resultado.IsValid);
    }
}
