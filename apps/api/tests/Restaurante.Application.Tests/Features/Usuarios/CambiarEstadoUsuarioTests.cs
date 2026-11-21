using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Usuarios;
using Restaurante.Application.Features.Usuarios.CambiarEstadoUsuario;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Usuarios;

public class CambiarEstadoUsuarioTests
{
    private readonly UsuarioRepositoryEnMemoria _repositorio = new();
    private readonly RelojFijo _reloj = new();
    private Guid _actual = Guid.NewGuid();

    private Usuario Agregar(string nombre, Rol rol)
    {
        var u = Usuario.Crear(nombre, "hash:x", rol, _reloj.Ahora);
        _repositorio.Agregar(u);
        return u;
    }

    private Task<UsuarioDto> Ejecutar(Guid id, bool activo) =>
        new CambiarEstadoUsuarioHandler(_repositorio, new UsuarioActualFalso(_actual))
            .Handle(new CambiarEstadoUsuarioCommand(id, activo), CancellationToken.None);

    [Fact(DisplayName = "001/RF-15: desactiva y reactiva")]
    public async Task DesactivaYReactiva()
    {
        var mesero = Agregar("pepe", Rol.Mesero);

        var dto = await Ejecutar(mesero.Id, false);
        Assert.False(dto.Activo);
        Assert.False(mesero.Activo);

        dto = await Ejecutar(mesero.Id, true);
        Assert.True(dto.Activo);
        Assert.True(mesero.Activo);
        Assert.Equal(2, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "001/RF-17: no desactiva al último administrador activo")]
    public async Task NoDesactivaUltimoAdministrador()
    {
        Agregar("admin", Rol.Administrador).Desactivar();
        var unico = Agregar("jefe", Rol.Administrador);

        await Assert.ThrowsAsync<ConflictoException>(() => Ejecutar(unico.Id, false));

        Assert.True(unico.Activo);
        Assert.Equal(0, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "001/RF-17: sí desactiva a un administrador si queda otro activo")]
    public async Task DesactivaAdministradorSiQuedaOtro()
    {
        Agregar("admin", Rol.Administrador);
        var otro = Agregar("jefe", Rol.Administrador);

        var dto = await Ejecutar(otro.Id, false);

        Assert.False(dto.Activo);
    }

    [Fact(DisplayName = "001/RF-17: reactivar no activa la regla del último administrador")]
    public async Task ReactivarNoActivaRegla()
    {
        var inactivo = Agregar("admin", Rol.Administrador);
        inactivo.Desactivar();

        var dto = await Ejecutar(inactivo.Id, true);

        Assert.True(dto.Activo);
    }

    [Fact(DisplayName = "001/RF-18: un administrador no puede desactivarse a sí mismo")]
    public async Task NoSeDesactivaASiMismo()
    {
        Agregar("admin", Rol.Administrador);
        var yo = Agregar("jefe", Rol.Administrador);
        _actual = yo.Id;

        await Assert.ThrowsAsync<ConflictoException>(() => Ejecutar(yo.Id, false));

        Assert.True(yo.Activo);
    }

    [Fact(DisplayName = "001/RF-15: el validador exige el estado (activo) con el mensaje en español")]
    public void ValidadorExigeActivo()
    {
        var resultado = new CambiarEstadoUsuarioValidator().Validate(new CambiarEstadoUsuarioCommand(Guid.NewGuid(), null));

        var error = Assert.Single(resultado.Errors);
        Assert.Equal("Activo", error.PropertyName);
        Assert.Equal("El estado (activo) es obligatorio", error.ErrorMessage);
    }

    [Theory(DisplayName = "001/RF-15: el validador acepta activo verdadero o falso")]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidadorAceptaActivo(bool activo)
    {
        var resultado = new CambiarEstadoUsuarioValidator().Validate(new CambiarEstadoUsuarioCommand(Guid.NewGuid(), activo));

        Assert.True(resultado.IsValid);
    }

    [Fact(DisplayName = "001/RF-19: id inexistente lanza NoEncontradoException")]
    public async Task IdInexistenteLanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() => Ejecutar(Guid.NewGuid(), false));
    }
}
