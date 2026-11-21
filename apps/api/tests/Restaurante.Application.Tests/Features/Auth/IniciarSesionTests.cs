using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Auth.IniciarSesion;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Auth;

public class IniciarSesionTests
{
    private readonly UsuarioRepositoryEnMemoria _repositorio = new();
    private readonly HasherFalso _hasher = new();
    private readonly RelojFijo _reloj = new();

    private IniciarSesionHandler CrearHandler() => new(_repositorio, _hasher, new EmisorTokensFalso());

    private Usuario Agregar(string nombre, string contrasena, Rol rol, bool activo = true)
    {
        var usuario = Usuario.Crear(nombre, _hasher.Hashear(contrasena), rol, _reloj.Ahora);
        if (!activo)
        {
            usuario.Desactivar();
        }

        _repositorio.Agregar(usuario);
        return usuario;
    }

    [Fact(DisplayName = "001/RF-01: credenciales válidas devuelven token y rol")]
    public async Task CredencialesValidasDevuelvenTokenYRol()
    {
        Agregar("Ana", "secreta123", Rol.Administrador);

        var respuesta = await CrearHandler().Handle(new IniciarSesionCommand("Ana", "secreta123"), CancellationToken.None);

        Assert.Equal("token-de-Ana", respuesta.Token);
        Assert.Equal(EmisorTokensFalso.Expiracion, respuesta.ExpiraEn);
        Assert.Equal("Ana", respuesta.NombreUsuario);
        Assert.Equal("administrador", respuesta.Rol);
    }

    [Fact(DisplayName = "001/RF-01: el nombre se busca sin distinguir mayúsculas")]
    public async Task NombreSeBuscaSinDistinguirMayusculas()
    {
        Agregar("admin", "secreta123", Rol.Administrador);

        var respuesta = await CrearHandler().Handle(new IniciarSesionCommand("ADMIN", "secreta123"), CancellationToken.None);

        Assert.Equal("admin", respuesta.NombreUsuario);
    }

    [Fact(DisplayName = "001/RF-01: el nombre se recorta antes de buscar")]
    public async Task NombreSeRecortaAntesDeBuscar()
    {
        Agregar("admin", "secreta123", Rol.Caja);

        var respuesta = await CrearHandler().Handle(new IniciarSesionCommand("  admin \t", "secreta123"), CancellationToken.None);

        Assert.Equal("admin", respuesta.NombreUsuario);
        Assert.Equal("caja", respuesta.Rol);
    }

    [Fact(DisplayName = "001/RF-02: usuario inexistente y contraseña incorrecta dan la misma excepción y mensaje")]
    public async Task InexistenteEIncorrectaDanLaMismaExcepcion()
    {
        Agregar("ana", "secreta123", Rol.Mesero);

        var inexistente = await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            CrearHandler().Handle(new IniciarSesionCommand("nadie", "secreta123"), CancellationToken.None));
        var incorrecta = await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            CrearHandler().Handle(new IniciarSesionCommand("ana", "otra-clave"), CancellationToken.None));

        Assert.Equal("Usuario o contraseña incorrectos", inexistente.Message);
        Assert.Equal(inexistente.Message, incorrecta.Message);
    }

    [Fact(DisplayName = "001/RF-03: usuario desactivado da CredencialesInvalidasException")]
    public async Task UsuarioDesactivadoDaCredencialesInvalidas()
    {
        Agregar("ana", "secreta123", Rol.Cocina, activo: false);

        var ex = await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            CrearHandler().Handle(new IniciarSesionCommand("ana", "secreta123"), CancellationToken.None));

        Assert.Equal("Usuario o contraseña incorrectos", ex.Message);
    }

    private sealed class HasherEspia : IHasherContrasenas
    {
        public int Verificaciones { get; private set; }

        public string Hashear(string contrasena) => "hash:" + contrasena;

        public bool Verificar(string hash, string contrasena)
        {
            Verificaciones++;
            return hash == "hash:" + contrasena;
        }
    }

    [Fact(DisplayName = "001/RF-02: usuario inexistente también ejecuta Verificar (sin canal de tiempo)")]
    public async Task InexistenteTambienVerificaContrasena()
    {
        var espia = new HasherEspia();
        var handler = new IniciarSesionHandler(_repositorio, espia, new EmisorTokensFalso());

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            handler.Handle(new IniciarSesionCommand("nadie", "secreta123"), CancellationToken.None));

        Assert.Equal(1, espia.Verificaciones);
    }

    [Fact(DisplayName = "001/RF-03: usuario desactivado también ejecuta Verificar y da el mismo error")]
    public async Task DesactivadoTambienVerificaContrasena()
    {
        var espia = new HasherEspia();
        Agregar("ana", "secreta123", Rol.Cocina, activo: false);
        var handler = new IniciarSesionHandler(_repositorio, espia, new EmisorTokensFalso());

        var ex = await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            handler.Handle(new IniciarSesionCommand("ana", "secreta123"), CancellationToken.None));

        Assert.Equal(1, espia.Verificaciones);
        Assert.Equal("Usuario o contraseña incorrectos", ex.Message);
    }

    [Theory(DisplayName = "001/RF-04: nombre o contraseña vacíos no superan el validator")]
    [InlineData("", "secreta123", "NombreUsuario")]
    [InlineData("   ", "secreta123", "NombreUsuario")]
    [InlineData("ana", "", "Contrasena")]
    [InlineData("ana", "   ", "Contrasena")]
    public void VaciosNoSuperanElValidator(string nombre, string contrasena, string campo)
    {
        var resultado = new IniciarSesionValidator().Validate(new IniciarSesionCommand(nombre, contrasena));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    [Fact(DisplayName = "001/RF-04: credenciales con datos presentes superan el validator")]
    public void DatosPresentesSuperanElValidator()
    {
        var resultado = new IniciarSesionValidator().Validate(new IniciarSesionCommand("ana", "x"));

        Assert.True(resultado.IsValid);
    }
}
